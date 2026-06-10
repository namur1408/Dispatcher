Shader "UI/SignalLight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [Header(Light Colors and Glow)]
        [HDR] _Color ("Halo Color", Color) = (1.0, 0.2, 0.0, 1.0)
        [HDR] _CoreColor ("Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _HaloSpread ("Halo Spread (Lower = Wider)", Range(1.0, 20.0)) = 4.0
        _CoreSize ("Core Size (Higher = Smaller)", Range(10.0, 100.0)) = 40.0
        
        [Header(Blinking Animation)]
        _BlinkSpeed ("Blink Speed", Float) = 3.0
        _BlinkSharpness ("Blink Sharpness (1=Smooth, 20=Strobe)", Range(1.0, 50.0)) = 2.0
        _MinIntensity ("Min Intensity", Range(0.0, 1.0)) = 0.1
        _MaxIntensity ("Max Intensity", Range(0.0, 10.0)) = 2.0
        
        [Header(Synchronization)]
        _PhaseOffset ("Manual Phase Offset", Float) = 0.0
        _AutoPhase ("Auto Desync (by Position)", Range(0.0, 5.0)) = 1.0

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
        // Режим аддитивного смешивания (свет складывается с фоном)
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
            fixed4 _CoreColor;
            float _HaloSpread;
            float _CoreSize;
            
            float _BlinkSpeed;
            float _BlinkSharpness;
            float _MinIntensity;
            float _MaxIntensity;
            
            float _PhaseOffset;
            float _AutoPhase;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                
                // Получаем мировые координаты для рассинхронизации мерцания
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPosition.xy;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Переводим UV из [0..1] в [-1..1] (центр картинки будет в 0,0)
                float2 uv = (i.uv - 0.5) * 2.0;
                float dist = length(uv);

                // Экспоненциальное затухание дает оптически правильное свечение (светорассеяние в линзе/тумане)
                float halo = exp(-dist * _HaloSpread);
                float core = exp(-dist * _CoreSize);

                // --- Анимация мерцания ---
                // Автоматический сдвиг фазы на основе позиции объекта на экране (чтобы огни мигали вразнобой)
                float autoPhaseOffset = (i.worldPos.x + i.worldPos.y) * 0.01 * _AutoPhase;
                float time = _Time.y * _BlinkSpeed + _PhaseOffset + autoPhaseOffset;
                
                // Генерируем пульс от 0 до 1
                float pulse = max(0.0, sin(time));
                // Sharpness делает из плавной синусоиды резкие вспышки (strobe effect)
                pulse = pow(pulse, _BlinkSharpness);

                // Итоговая интенсивность в этот кадр
                float intensity = lerp(_MinIntensity, _MaxIntensity, pulse);

                // --- Сборка цвета ---
                float3 baseColor = _Color.rgb * halo + _CoreColor.rgb * core;
                baseColor *= intensity;

                // Плавно гасим свет у самых краев квадрата (чтобы не было жестких обрезанных углов)
                float edgeFade = smoothstep(1.0, 0.7, dist);
                float alpha = max(halo, core) * intensity * edgeFade;

                // Умножаем на базовый цвет компонента Image
                return fixed4(baseColor * i.color.rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
