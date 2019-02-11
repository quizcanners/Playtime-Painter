Shader "Playtime Painter/UI/Rounded/Box"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Sharpness", Range(0,1)) = 0.5
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

				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
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
					o.texcoord.z =		3 - _Edges * 2;//abs(o.texcoord.z);
					o.projPos.xy =		v.normal.xy;
					o.projPos.zw =		max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w =	1 / (1.0001 - o.texcoord.w);
					o.precompute.xy =	1 / (1.0001 - o.projPos.zw);
					o.precompute.z =	(1 + o.texcoord.z *  (16 - _Edges * 15));


					o.offUV.xy =		(o.texcoord.xy - 0.5)*2;
			
					return o;
				}

			

				float4 frag(v2f i) : COLOR{

					float4 _ProjTexPos =	i.projPos;
					float _Courners =		i.texcoord.w;
					float deCourners =		i.precompute.w;
					float2 uv =				abs(i.offUV);

					uv = max(0, uv - _ProjTexPos.zw) * i.precompute.xy - _Courners;
					uv = max(0, uv) * deCourners;

					float clipp = max(0, 1 - dot(uv,uv));

					clipp = min(1, pow(clipp * i.precompute.z, i.texcoord.z));

					i.color.a *= clipp;

					return i.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
