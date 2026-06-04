Shader "Custom/Raindrop" {
	Properties {
		iChannel0("Albedo (RGB)", 2D) = "white" {}
		_DropTint("Drop Tint", Color) = (0.7, 0.8, 1.0, 0.08)
		_EdgeGlow("Edge Glow", Range(0, 1)) = 0.2
		_DropScale("Drop Scale", Range(0.5, 6.0)) = 2.5
		_RainAmount("Rain Amount", Range(0, 1)) = 0.06
		_Distortion("Distortion", Range(0, 0.05)) = 0.012
		_Refraction("Refraction Blur", Range(0, 6)) = 3.0
		_Wind("Wind (Tilt)", Range(-0.5, 0.5)) = 0.1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Pass {
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D iChannel0;
			float4 _DropTint;
			float _EdgeGlow;
			float _DropScale;
			float _RainAmount;
			float _Distortion;
			float _Refraction;
			float _Wind;

			#define S(a, b, t) smoothstep(a, b, t)

			float3 N13(float p) {
				float3 p3 = frac(float3(p, p, p) * float3(.1031, .11369, .13787));
				p3 += dot(p3, p3.yzx + 19.19);
				return frac(float3((p3.x + p3.y) * p3.z, (p3.x + p3.z) * p3.y, (p3.y + p3.z) * p3.x));
			}

			float N(float t) {
				return frac(sin(t * 12345.564) * 7658.76);
			}

			float Saw(float b, float t) {
				return S(0., b, t) * S(1., b, t);
			}

			float3 DropLayer(float2 uv, float t, float amount) {
				float2 UV = uv;
				uv.y += t * 0.75;
				float2 a = float2(6., 1.);
				float2 grid = a * 2.;
				float2 id = floor(uv * grid);

				float colShift = N(id.x);
				uv.y += colShift;

				id = floor(uv * grid);
				float3 n = N13(id.x * 35.2 + id.y * 2376.1);
				float2 st = frac(uv * grid) - float2(.5, 0);

				if (n.y > amount) return float3(0, 0, 0);

				float x = (n.x - .5) * .7;

				float ti = frac(t + n.z);
				float y = (Saw(.85, ti) - .5) * .9 + .5;

				float d = length((st - float2(x, y)) * a.yx);

				float drop = S(.18, .14, d);
				float edge = S(.21, .17, d) - drop;

				float cd = abs(st.x - x);
				float trailFront = S(-.02, .02, st.y - y);
				float trail = S(.08, .03, cd) * trailFront * S(0., .5, st.y - y);

				return float3(drop, edge, trail);
			}

			float StaticDrops(float2 uv, float t, float amount) {
				uv *= 12.;
				float2 id = floor(uv);
				uv = frac(uv) - .5;
				float3 n = N13(id.x * 107.45 + id.y * 3543.654);
				if (n.y > amount) return 0.;
				float2 p = (n.xy - .5) * .7;
				float d = length(uv - p);
				float fade = Saw(.025, frac(t + n.z));
				return S(.16, .12, d) * fade;
			}



			fixed4 frag(v2f_img i) : SV_Target {
				float2 uv = ((i.uv * _ScreenParams.xy) - .5 * _ScreenParams.xy) / _ScreenParams.y;
				float2 UV = i.uv.xy;
				float T = _Time.y;
				float t = T * .2;

				uv *= _DropScale;
				
				float s = sin(_Wind);
				float c = cos(_Wind);
				uv = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);

				float sd = StaticDrops(uv, t, _RainAmount);
				float3 dl = DropLayer(uv, t, _RainAmount);

				float drops = sd + dl.x;
				float edges = dl.y;
				float trail = dl.z;

				float allDrops = drops + trail * 0.5;

				float2 n = float2(ddx(allDrops), ddy(allDrops));
				n *= _Distortion / 0.003;

				float blur = lerp(_Refraction, 0., S(.05, .15, allDrops));
				float4 texCoord = float4(UV.x + n.x, UV.y + n.y, 0, blur);
				float3 col = tex2Dlod(iChannel0, texCoord).rgb;

				col += _DropTint.rgb * drops * _DropTint.a;
				col += _DropTint.rgb * trail * _DropTint.a * 0.3;
				col += float3(0.6, 0.7, 0.9) * edges * _EdgeGlow;

				float fade = S(0., 10., T);
				col *= 1. - dot(UV -= .5, UV) * 0.5;
				col *= fade;

				return fixed4(col, 1);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}