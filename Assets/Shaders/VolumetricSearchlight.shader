Shader "UI/VolumetricSearchlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Light Color", Color) = (1.0, 0.95, 0.8, 1.0)
        
        [Header(Beam Shape)]
        _BaseWidth ("Base Width", Range(0.0, 0.5)) = 0.02
        _BeamSpread ("Beam Spread", Range(0.0, 2.0)) = 0.4
        _EdgeSoftness ("Edge Softness", Range(0.01, 1.0)) = 0.3
        
        [Header(Fading and Intensity)]
        _Falloff ("Vertical Falloff", Range(0.1, 5.0)) = 2.0
        _CoreGlow ("Core Glow Intensity", Range(0.0, 5.0)) = 1.0
        _Intensity ("Overall Intensity", Range(0.1, 10.0)) = 1.5
        
        [Header(Atmospheric Dust)]
        _NoiseScale ("Dust Scale", Float) = 15.0
        _NoiseSpeed ("Dust Speed", Float) = 0.3
        _NoiseStrength ("Dust Strength", Range(0, 1)) = 0.4

        [Header(Automatic Swing Animation)]
        _SwingSpeed ("Swing Speed", Float) = 1.5
        _SwingAngle ("Swing Angle (Max)", Range(0.0, 1.5)) = 0.3
        _PhaseOffset ("Manual Phase Offset", Float) = 0.0
        _AutoSwingPhase ("Auto Desync (by Position)", Range(0.0, 5.0)) = 1.0

        // Required for UI Masking
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        // Additive blending (Сложение цветов для реалистичного света)
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _BaseWidth;
            float _BeamSpread;
            float _EdgeSoftness;
            float _Falloff;
            float _CoreGlow;
            float _Intensity;
            
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseStrength;

            float _SwingSpeed;
            float _SwingAngle;
            float _PhaseOffset;
            float _AutoSwingPhase;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color; 
                
                // Получаем мировые координаты для рассинхронизации движения
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPosition.xy;
                
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i + float2(0.0, 0.0)), hash(i + float2(1.0, 0.0)), f.x),
                    lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x),
                    f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // --- Анимация движения (Вращение UV вокруг верхней точки 0.5, 1.0) ---
                float autoPhase = (i.worldPos.x + i.worldPos.y) * 0.01 * _AutoSwingPhase;
                float time = _Time.y * _SwingSpeed + _PhaseOffset + autoPhase;
                
                // Угол поворота (синусоида) + небольшой шум, чтобы движение не было идеально механическим
                float swingRandom = sin(time * 0.3) * 0.2; 
                float angle = sin(time) * _SwingAngle + swingRandom * _SwingAngle;
                
                float s = sin(angle);
                float c = cos(angle);
                
                // Смещаем центр вращения в верхнюю центральную точку (источник света)
                uv -= float2(0.5, 1.0);
                // Вращаем
                uv = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
                // Возвращаем центр на место
                uv += float2(0.5, 1.0);
                // -----------------------------------------------------------------

                float depth = 1.0 - uv.y; // depth = 0 на самом верху, 1 в самом низу
                
                // Исправление артефакта "обратного конуса": отсекаем всё, что находится "выше" источника света
                float cutoff = step(0.0, depth); 
                
                float distFromCenter = abs(uv.x - 0.5); // расстояние от центра луча

                // Расчет формы конуса (ограничиваем минимальные значения во избежание инверсии smoothstep)
                float currentWidth = max(0.0001, _BaseWidth + depth * _BeamSpread);
                float softness = max(0.0001, _EdgeSoftness * (0.05 + depth));

                // Затухание по бокам
                float horizontalFade = smoothstep(currentWidth + softness, currentWidth, distFromCenter);

                // Яркое ядро
                float core = smoothstep(currentWidth * 0.4, 0.0, distFromCenter) * _CoreGlow * pow(saturate(1.0 - depth), 0.5);

                // Затухание света к низу
                float verticalFade = pow(saturate(1.0 - depth), _Falloff);

                // Туман/пыль
                float2 noiseUV1 = i.uv * _NoiseScale + float2(_Time.y * _NoiseSpeed * 0.2, _Time.y * _NoiseSpeed);
                float2 noiseUV2 = i.uv * _NoiseScale * 1.5 + float2(-_Time.y * _NoiseSpeed * 0.1, _Time.y * _NoiseSpeed * 1.2);
                float n = (noise2D(noiseUV1) + noise2D(noiseUV2)) * 0.5;
                float dust = lerp(1.0, n, _NoiseStrength);

                // Итоговая альфа с учетом отсечения верхней части
                float alpha = (horizontalFade + core) * verticalFade * dust * _Intensity * cutoff;

                // Плавное скрытие луча по краям самой картинки (чтобы не резалось прямоугольником)
                alpha *= smoothstep(1.0, 0.9, 1.0 - i.uv.y); // Низ
                alpha *= smoothstep(0.0, 0.1, i.uv.x) * smoothstep(1.0, 0.9, i.uv.x); // Бока

                fixed4 finalColor = i.color;
                finalColor.a *= alpha;

                return finalColor;
            }
            ENDCG
        }
    }
}
