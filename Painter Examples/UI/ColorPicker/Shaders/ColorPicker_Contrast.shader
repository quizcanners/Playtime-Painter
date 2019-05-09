Shader "Playtime Painter/UI/ColorPicker/Contrast" {
	Properties{
		[PerRendererData]_MainTex("Mask (RGB)", 2D) = "white" {}
		[NoScaleOffset]_Circle("Circle", 2D) = "black" {}
		_NoiseMask("NoiseMask (RGB)", 2D) = "gray" {}
		_SomeSlider("Reflectiveness or something", Range(0.23,0.27)) = 0
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZTest Off
		ZWrite Off

		SubShader{

			Pass{

				CGPROGRAM
				#include "Assets/Tools/Playtime_Painter/Shaders/quizcanners_cg.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _Circle;
				sampler2D _NoiseMask;
				float _SomeSlider;

				struct v2f {
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD2;
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord.xy;
					return o;
				}

				float4 frag(v2f o) : COLOR{

					float4 col = tex2D(_MainTex, o.texcoord);

					col.rgb = HUEtoColor(1-_Picker_HUV + 0.2463);

					float4 noise = tex2Dlod(_NoiseMask, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));

					float2 uv = saturate(o.texcoord.xy +(noise.xy - 0.5)*0.015);

					//uv = (uv - 0.5)*2;
					//uv = uv*abs(uv)*0.5+0.5;

					uv = pow(uv,1.5);

					col.rgb = uv.y + col.rgb*(1- uv.y);

					col.rgb *= uv.x*uv.x;

					float2 dist = (o.texcoord - float2(_Picker_Brightness, 1-_Picker_Contrast))*8;

					

					float ca = max(0, 1-max(0, length(abs(dist)) - 0.5) * 32);

					//return ca;

				    float4 circle = tex2D(_Circle, dist+0.5);

					ca *=  circle.a;

					//col.rgb += saturate(1 - length(dist));

					col.rgb = col.rgb*(1 - ca) + circle.rgb*ca;

					return col;
				}
					ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

