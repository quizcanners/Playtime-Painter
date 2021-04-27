Shader "Playtime Painter/Buffer Blit/Pixel Perfect Copy" {
	Properties{
		 _MainTex("Tex", 2D) = "white" {}
	}
	
	Category{
		
		ColorMask RGBA
		Cull Off
		ZTest off
		ZWrite off

		SubShader{

			Tags{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
			}

			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "PlaytimePainter cg.cginc"

				sampler2D _MainTex;

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;  
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);    // Position on the screen
					o.texcoord.xy = v.texcoord;

					return o;
				}

				float4 frag(v2f i) : COLOR{

					float2 perfTex = (floor(i.texcoord.xy*_qcPp_BufferSourceTexelSize.zw) + 0.5) * _qcPp_BufferSourceTexelSize.xy;

					float4 col = tex2Dlod(_MainTex, float4(perfTex, 0, 0));

					return col;

				}
				ENDCG
			}
		}
	}
}
