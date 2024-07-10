﻿Shader "Playtime Painter/Buffer Blit/DownScaleX32_Approx" {
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
				//float4 _MainTex_TexelSize;

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

				float4 GrabMain(float2 uv) {

					float4 tex = tex2Dlod(_MainTex, float4(uv, 0, 0));

					#if UNITY_COLORSPACE_GAMMA
						tex.rgb = pow(tex.rgb, GAMMA_TO_LINEAR);
					#endif

					return tex;
				}

				float4 frag(v2f i) : COLOR{

					float2 uv = i.texcoord.xy;

					uv = (floor(uv*_qcPp_BufferSourceTexelSize.zw) + 0.5) * _qcPp_BufferSourceTexelSize.xy;

			
					// Central 4

					float2 checkerOff = _qcPp_BufferSourceTexelSize.xy * 4; // instead if *1

					float4 col = GrabMain(uv + checkerOff) + GrabMain(uv - checkerOff);

					checkerOff.x = -checkerOff.x;

					col += GrabMain(uv + checkerOff) + GrabMain(uv - checkerOff);

					float2 checkerOff2 = checkerOff * 3; // To sample the center of next 4x4 group

					// Courner 4
					col += GrabMain(uv + checkerOff2) + GrabMain(uv - checkerOff2);

					checkerOff2.x = -checkerOff2.x;

					col += GrabMain(uv + checkerOff2) + GrabMain(uv - checkerOff2);

					// Top And Bottom
					float2 checkerOffd = float2(checkerOff.x, checkerOff2.y);

					col += GrabMain(uv + checkerOffd) + GrabMain(uv - checkerOffd);

					checkerOffd.x = -checkerOffd.x;

					col += GrabMain(uv + checkerOffd) + GrabMain(uv - checkerOffd);

					// Left And right
					checkerOffd = float2(checkerOff2.x, checkerOff.y);

					col += GrabMain(uv + checkerOffd) + GrabMain(uv - checkerOffd);

					checkerOffd.x = -checkerOffd.x;

					col += GrabMain(uv + checkerOffd) + GrabMain(uv - checkerOffd);


					col *= 0.0625;  // /16

					#if UNITY_COLORSPACE_GAMMA
					col.rgb = pow(col.rgb, LINEAR_TO_GAMMA);
					#endif

					return col;
				}
				ENDCG
			}
		}
	}
}