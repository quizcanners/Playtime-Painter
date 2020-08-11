Shader "Playtime Painter/UI/Rounded/Unlinked/Box Unlinked"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Sharpness", Range(0,1)) = 0.5
		[Toggle(TRIMMED)] trimmed("Trimmed Corners", Float) = 0
	}
	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Simple"
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

					#if TRIMMED
						o.texcoord.w *= 0.9f;
					#endif


					o.precompute.w =	1 / (1.0001 - o.texcoord.w);
					o.precompute.xy =	1 / (1.0001 - o.projPos.zw);
					o.precompute.z =	1 + _Edges * 32;

					o.offUV.xy =		(o.texcoord.xy - 0.5)*2;
			
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

					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;

					float2 forFade = uv;

					uv = max(0, uv - _Courners) * something;

					#if TRIMMED
						float dist = (uv.x + uv.y); 

						dist = dist * (deCourners * 0.7) + deCourners * 0.25 + _Courners * 0.9;
		
					#else
						float dist = dot(uv, uv);
					#endif

					#if !TRIMMED 
						forFade *= forFade;
					#endif

					float fade = max(forFade.x, forFade.y);

					float alpha = max(0, 1 - max(fade, dist));

					alpha = min(1, pow(alpha * o.precompute.z, o.texcoord.z));

					o.color.a *= alpha;

					return o.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
