Shader "Playtime Painter/UI/Rounded/Box"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Sharpness", Range(0,1)) = 0.5
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
				#pragma target 3.0

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

					o.precompute.w =	1 / (1.0001 - o.texcoord.w);
					o.precompute.xy =	1 / (1.0001 - o.projPos.zw);
					o.precompute.z =	(1 + _Edges * 32);

					o.offUV.xy =		(o.texcoord.xy - 0.5)*2;
			
					return o;
				}

			

				float4 frag(v2f o) : COLOR{

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners =		o.precompute.w;
					float2 uv =				abs(o.offUV);


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
