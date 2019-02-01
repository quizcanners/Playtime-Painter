Shader "Playtime Painter/UI/SoftButtonShadow" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
	}
	Category{
		Tags{
			"Queue" = "Transparent-500"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Position"
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
					float4 texcoord : TEXCOORD2;
					float4 color: COLOR;
					float4 projPos : TEXCOORD6;
				};

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = abs(o.texcoord.z)*10;
					o.projPos.xy = v.normal.xy;
					o.projPos.zw = max(0, float2(v.normal.z, -v.normal.z));

					return o;
				}

				float4 frag(v2f i) : COLOR{

					float4 _ProjTexPos = i.projPos;
					float _Edge = i.texcoord.z;
					float _Courners = i.texcoord.w;

					float _Blur = (1 - i.color.a);
					float2 uv = abs(i.texcoord.xy - 0.5) * 2;

					uv = max(0, uv - _ProjTexPos.zw) / (1 - _ProjTexPos.zw + 0.0001) - _Courners;
					float deCourners = 1.0001 - _Courners;
					uv = max(0, uv) / deCourners;

					uv *= uv;

					float clipp = max(0, (1 - uv.x - uv.y));

					float4 col = i.color;

					col.a *= pow(clipp, _Edge) *saturate((1 - clipp) * 10) * saturate((i.color.a - 0.8) * 5);

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
