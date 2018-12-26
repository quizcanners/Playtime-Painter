Shader "Playtime Painter/Basic/VertexColor" {
	Properties{}
	Category{
		Tags{
			"Queue" = "AlphaTest"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma target 3.0

				float4  _MainTex_ST;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 color: COLOR;
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					return o;
				}

				float4 frag(v2f i) : COLOR{
					i.color.a = 1;
					return i.color;
				}
				ENDCG

			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}

}

