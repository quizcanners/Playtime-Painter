﻿Shader "Playtime Painter/Editor/Buffer Blit/Copier" {
	Properties{
		 _MainTex("Tex", 2D) = "white" {}
	}
	
	Category{
		Tags{ 
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}

		ColorMask RGBA
		Cull Back
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "PlaytimePainter cg.cginc"

				sampler2D _MainTex;

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;  
				};

				v2f vert(appdata_brush_qc v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);   
					o.texcoord = brushTexcoord (v.texcoord.xy, v.vertex);

					return o;
				}

				float4 frag(v2f i) : SV_Target{
					return tex2Dlod(_MainTex, float4(i.texcoord.xy, 0, 0));
				}
				ENDCG
			}
		}
	}
}