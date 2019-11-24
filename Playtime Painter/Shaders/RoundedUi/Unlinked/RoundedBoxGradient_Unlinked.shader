Shader "Playtime Painter/UI/Rounded/Unlinked/Gradient Unlinked"
{
	Properties{
		[PerRendererData][NoScaleOffset]_MainTex("Noise (RGB)", 2D) = "gray" {}
		_Edges("Sharpness", Range(0,1)) = 0.5
		_ColorC("Center", Color) = (1,1,1,1)
		_ColorE("Edge", Color) = (1,1,1,1)

		[KeywordEnum(Hor, Vert)] _GRAD("Gradient Direction (Feature)", Float) = 0
		[KeywordEnum(Once, Mirror)] _GRADS("Gradient Spread (Feature)", Float) = 0
		[Toggle(TRIMMED)] trimmed("Trimmed Corners", Float) = 0
		[Toggle(FADE)] faded("Fade", Float) = 0
		_FadeEdge("Fade Sharpness", Range(0,200)) = 10
	}

	Category{

		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "FadePosition"
			"SpriteRole" = "Hide"
			"PerEdgeData" = "Unlinked"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile_instancing
				#pragma multi_compile ___ USE_NOISE_TEXTURE
				#pragma shader_feature __ TRIMMED
				#pragma shader_feature __ FADE

				#pragma shader_feature _GRAD_HOR _GRAD_VERT 
				#pragma shader_feature _GRADS_ONCE _GRADS_MIRROR


				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 projPos : TEXCOORD1;
					float4 precompute : TEXCOORD2;
					float4 offUV : TEXCOORD3;
					float4 screenPos :	TEXCOORD5;
#if FADE
					float4 fade : TEXCOORD4;
					
#endif
					float4 color: COLOR;
				};

				float _FadeEdge;
				float _Edges;
				float4 _ColorC;
				float4 _ColorE;
				sampler2D _Global_Noise_Lookup;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.texcoord.xy =		v.texcoord.xy;
					o.color =			v.color;

					o.texcoord.zw =		v.texcoord1.xy;
					o.texcoord.z =		4 - _Edges * 3;
					o.projPos.xy =		v.normal.xy;
					o.projPos.zw =		max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.screenPos = ComputeScreenPos(o.pos);

					#if FADE
					
					o.fade = float4(v.texcoord2.xy, v.texcoord3.xy);
					#endif

					#if TRIMMED
						o.texcoord.w *= 0.9f;
					#endif

					o.precompute.w =	1 / (1.0001 - o.texcoord.w);
					o.precompute.xy =	1 / (1.0001 - o.projPos.zw);
					o.precompute.z =	1 + _Edges*32;

					o.offUV.xy = (o.texcoord.xy - 0.5) * 2;

					o.offUV.zw = float2((o.offUV.x + _SinTime.x) * 987.432, (o.offUV.y + _CosTime.x) * 123.456);

					return o;
				}



				float4 frag(v2f o) : COLOR{

					float dx = abs(ddx(o.texcoord.x));
					float dy = abs(ddy(o.texcoord.y));
					float mip = (dx + dy) * 200;

					_Edges /= 1 + mip * mip; //LOD

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners = 1 - _Courners;
					float something =		o.precompute.w;
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

					#if !_GRADS_ONCE 
					mid *= 2;
					mid = (mid*mid)/4;
					#endif

#ifdef UNITY_COLORSPACE_GAMMA
					_ColorC.rgb *= _ColorC.rgb;
					_ColorE.rgb *= _ColorE.rgb;
#endif

					o.color.rgb = _ColorE.rgb * (mid)+_ColorC.rgb * (1 - mid);


					#ifdef UNITY_COLORSPACE_GAMMA
						o.color.rgb = sqrt(o.color.rgb);
					#endif

					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;
									
					float2 forFade = uv;

					uv = max(0, uv - _Courners) * something;

					#if TRIMMED
						float dist = (uv.x + uv.y);

						dist = dist * (deCourners * 0.7) + deCourners * 0.25 + _Courners * 0.9;
					
					#else
						float dist = dot(uv, uv);
						forFade *= forFade;
					#endif

					float fade = max(forFade.x, forFade.y);

					float alpha = max(0, 1 - max(fade, dist));

					alpha = min(1, pow(alpha * o.precompute.z, o.texcoord.z));

					float4 col = o.color;

					col.a *= alpha;

					float2 sUV = o.screenPos.xy / o.screenPos.w;

					#if FADE

							

							float edge =  1-
								saturate((sUV.x - o.fade.x) * _FadeEdge)
								* saturate((sUV.y - o.fade.y) * _FadeEdge)
								* saturate((o.fade.z - sUV.x) * _FadeEdge)
								* saturate((o.fade.w - sUV.y) * _FadeEdge)
								;
							
							edge *= edge;

							col.a *= 1- edge;

							//o.fade = float4(v.texcoord2.xy, v.texcoord3.zw);
					#endif


					#if USE_NOISE_TEXTURE
							float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(sUV * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						#ifdef UNITY_COLORSPACE_GAMMA
							col.rgb += (noise.rgb - 0.5)*0.02*(3 - col.a * 2);;
						#else
							col.rgb += (noise.rgb - 0.5)*0.0075*(3 - col.a * 2);
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
