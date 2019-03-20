Shader "Playtime Painter/UI/Rounded/Gradient"
{
	Properties{
		[PerRendererData][NoScaleOffset]_MainTex("Noise (RGB)", 2D) = "gray" {}
		//[NoScaleOffset]_NoiseTex("Albedo (RGB)", 2D) = "gray" {}
		_Edges("Sharpness", Range(0,1)) = 0.5
		_ColorC("Center", Color) = (1,1,1,1)
		_ColorE("Edge", Color) = (1,1,1,1)

		[KeywordEnum(Hor, Vert)] _GRAD("Gradient Direction (Feature)", Float) = 0
		[KeywordEnum(Once, Mirror)] _GRADS("Gradient Spread (Feature)", Float) = 0
		[Toggle(_UNLINKED)] unlinked("Linked Corners", Float) = 0
	}
	Category{
		Tags{
			"Queue" = "Transparent+10"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Hide"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile_instancing
				#pragma multi_compile ____  _UNLINKED 
				#pragma multi_compile ___ USE_NOISE_TEXTURE

				#pragma shader_feature _GRAD_HOR _GRAD_VERT 
				#pragma shader_feature _GRADS_ONCE _GRADS_MIRROR


				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 projPos : TEXCOORD1;
					float4 precompute : TEXCOORD2;
					float4 offUV : TEXCOORD3;
					float4 color: COLOR;
				};

				float _Edges;
				float4 _ColorC;
				float4 _ColorE;
				sampler2D _Global_Noise_Lookup;
				//sampler2D _NoiseTex;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = 3 - _Edges * 2;
					o.projPos.xy = v.normal.xy;
					o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w = 1 / (1.0001 - o.texcoord.w);
					o.precompute.xy = 1 / (1.0001 - o.projPos.zw);
					o.precompute.z = (1 + _Edges*32);

					o.offUV.xy = (o.texcoord.xy - 0.5) * 2;

					o.offUV.zw = float2((o.offUV.x + _SinTime.x) * 987.432, (o.offUV.y + _CosTime.x) * 123.456);

					return o;
				}



				float4 frag(v2f o) : COLOR{

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners =		o.precompute.w;
					float2 uv =				abs(o.offUV);

					#if _GRADS_ONCE 
						
					float2 forGrad = o.texcoord.xy;

					#else 

					float2 forGrad = uv;

					#endif

					#if _GRAD_VERT
					float mid = forGrad.y;
					#else
					float mid = forGrad.x;
					#endif

					mid *= 2;

					mid = (mid*mid)/4;


#ifdef UNITY_COLORSPACE_GAMMA
					_ColorC.rgb *= _ColorC.rgb;
					_ColorE.rgb *= _ColorE.rgb;
#endif

					#if USE_NOISE_TEXTURE

					float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));

					noise.xy = (noise.xy -0.5) * 0.01;

					o.color.rgb = _ColorE.rgb * (mid + noise.x) + _ColorC.rgb * (1 - mid + noise.y);

					#else

					o.color.rgb = _ColorE.rgb * (mid) + _ColorC.rgb * (1 - mid);

					#endif

					#ifdef UNITY_COLORSPACE_GAMMA
					o.color.rgb = sqrt(o.color.rgb);
					#endif

					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;
									
					float2 forFade = uv;

					uv = max(0, uv - _Courners) * deCourners;

					#if _UNLINKED
					forFade *= forFade;
					float clipp = max(0, 1 - max(max(forFade.x, forFade.y), dot(uv, uv)));
					#else 
					float clipp = max(0, 1 - dot(uv, uv));
					#endif

					clipp = min(1, pow(clipp * o.precompute.z, o.texcoord.z));

					o.color.a *= clipp;

					return o.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
