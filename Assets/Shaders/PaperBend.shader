Shader "UI/PaperBend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _BendAmount ("Bend Amount", Range(-150, 150)) = 0
        _BendCenter ("Bend Center (UV.x)", Range(0, 1)) = 0.5
        _ShadowStrength ("Shadow on Fold", Range(0, 0.5)) = 0.15

        // UI stencil support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float  shadow : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _BendAmount;
            float _BendCenter;
            float _ShadowStrength;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // --- Bend ---
                // Расстояние от центра сгиба (в UV пространстве)
                float dist = v.uv.x - _BendCenter;

                // Парабола: прогиб пропорционален квадрату расстояния от центра
                float bend = dist * dist * _BendAmount;

                // Сдвигаем вершину вверх/вниз
                v.vertex.y += bend;

                // Лёгкий сдвиг по Z для глубины (необязательно, но даёт объём)
                v.vertex.z -= abs(bend) * 0.01;

                // Тень: чем ближе к центру сгиба, тем темнее
                float shadowMask = 1.0 - saturate(abs(dist) * 3.0) * _ShadowStrength * saturate(abs(_BendAmount) / 30.0);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.shadow = shadowMask;
                o.worldPos = v.vertex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Применяем тень от сгиба
                col.rgb *= i.shadow;

                // UI Clipping
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);

                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
}
