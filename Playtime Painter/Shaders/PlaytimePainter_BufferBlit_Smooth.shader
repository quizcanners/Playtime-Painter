Shader "Playtime Painter/Buffer Blit/Smooth" {
	Properties{
		_MainTex("Tex", 2D) = "white" {}
	}
	
	Category{
		Tags{ 
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}

		ColorMask RGBA
		Cull Off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "PlaytimePainter_cg.cginc"

				sampler2D _MainTex;

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				v2f vert(appdata_brush_qc v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord.xy;

					return o;
				}

				float4 frag(v2f i) : COLOR{
					float4 col = tex2Dlod(_MainTex, float4(i.texcoord.xy,0,0));
					return col;
				}
				ENDCG
			}
		}
	}
}