Shader "Playtime Painter/UI/Rounded/Outline"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Sharpness", Range(0.2,5)) = 0.5
		_Thickness("Thinnesss", Range(0.2,5)) = 1
		[Toggle(_UNLINKED)] unlinked("Linked Corners", Float) = 0
		[Toggle(TRIMMED)] trimmed("Trimmed Corners", Float) = 0
	}
	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Hide"
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
				#pragma shader_feature __ _UNLINKED 
				#pragma shader_feature __ TRIMMED

				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 projPos : TEXCOORD1;
					float4 precompute : TEXCOORD2;
					float2 offUV : TEXCOORD3;
					float4 color: COLOR;
				};

			
				float _Edges;
				float _Thickness;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.texcoord.xy =		v.texcoord.xy;
					o.color =			v.color;

					o.texcoord.zw =		v.texcoord1.xy;
					o.texcoord.w *= 0.99; // v.texcoord1.xy;
					o.texcoord.z =		0;
					o.projPos.xy =		v.normal.xy;
					o.projPos.zw =		max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w =	1 / (1.0001 - o.texcoord.w);
					o.precompute.xy =	1 / (1.0001 - o.projPos.zw);
					o.precompute.z =	(1 + _Edges * 32);

					o.offUV.xy =		(o.texcoord.xy - 0.5) * 2;

					return o;
				}


				float4 frag(v2f o) : COLOR{
					
					float4 _ProjTexPos = o.projPos;
					float _Courners = o.texcoord.w;
					float deCourners = 1 - _Courners;
					float something = o.precompute.w;
					float2 uv = abs(o.offUV);
				
					float _Blur = (1 - o.color.a);
					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;

					float2 forFade = uv;

					uv = max(0, uv - _Courners) * something;

					#if TRIMMED

					float dist = (uv.x + uv.y);

						#if _UNLINKED
							dist = dist * (deCourners * 0.7) + deCourners * 0.25 + _Courners*0.9;
						#else
							dist = dist * (deCourners * 0.85) + deCourners * 0.25 + _Courners * 0.9;
						#endif

					#else
						float dist = dot(uv, uv);
					#endif

					float exterior = 15;

					#if !TRIMMED
						forFade *= forFade;
					#endif

							float fade = max(forFade.x, forFade.y);

					#if _UNLINKED

							float clipp = max(0, min(1 - fade, 1 - dist) * _Thickness);

							float uvy = saturate(clipp * 8 *(1 + _Edges));

					#else 

						float clipp = max(0, 1 - dist)* _Thickness;
						float uvy = saturate(clipp * (8 - _Courners * 7)*(1 + _Edges));
					
						exterior *= something;
						
					#endif

						float outside = saturate((1 - uvy) * 2);
						
						o.color.a *= min(1, outside * 
							min(clipp * _Edges  * (1 - _Blur)*exterior, 1)//*(2 - _Edges)
							*(3 - uvy));

					return o.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
