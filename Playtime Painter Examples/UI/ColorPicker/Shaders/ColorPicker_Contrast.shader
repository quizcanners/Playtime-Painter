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

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0
				#pragma multi_compile ___ USE_NOISE_TEXTURE

				sampler2D _MainTex;
				sampler2D _Circle;
				sampler2D _NoiseMask;
				float _SomeSlider;
				float _Picker_Brightness;
				float _Picker_Contrast;
				float _Picker_HUV;
				sampler2D _Global_Noise_Lookup;

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

				inline float3 HUEtoColor(float hue) {

					float val = frac(hue + 0.082) * 6;

					float3 col;

					col.r = saturate(2 - abs(val - 2));

					val = fmod((val + 2), 6);

					col.g = saturate(2 - abs(val - 2));

					val = fmod((val + 2), 6);

					col.b = saturate(2 - abs(val - 2));

					col.rgb = pow(col.rgb, 2.2);

					return col;
				}

				float4 frag(v2f o) : COLOR{

					float4 col = tex2D(_MainTex, o.texcoord);

					col.rgb = HUEtoColor(1-_Picker_HUV + 0.2463);

					float2 uv = o.texcoord.xy;

					uv = pow(uv,1.5);

					col.rgb = uv.y + col.rgb*(1- uv.y);

					col.rgb *= uv.x*uv.x;

					float2 dist = (o.texcoord - float2(_Picker_Brightness, 1-_Picker_Contrast))*8;

					float ca = max(0, 1-max(0, length(abs(dist)) - 0.5) * 32);

				    float4 circle = tex2D(_Circle, dist+0.5);

					ca *=  circle.a;

					col.rgb = col.rgb*(1 - ca) + circle.rgb*ca;

					#if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						#ifdef UNITY_COLORSPACE_GAMMA
							col.rgb += (noise.rgb - 0.5)*0.02;
						#else
							col.rgb += (noise.rgb - 0.5)*0.0075;
						#endif
					#endif


					return col;
				}
					ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

