Shader "Playtime Painter/Buffer Blit/DownScaleX8" {
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

					float2 off = _qcPp_BufferSourceTexelSize.xy;

					// Central 4
					float4 col = GrabMain(uv + off) + GrabMain(uv - off);

					off.x = -off.x;

					col += GrabMain(uv + off) + GrabMain(uv - off);

					
					float2 off2 = off * 3; // To sample the center of next 2x2 group


					// Courner 4
					col += GrabMain(uv + off2) + GrabMain(uv - off2);

					off2.x = -off2.x;

					col += GrabMain(uv + off2) + GrabMain(uv - off2);

					// Top And Bottom
					float2 offd = float2(off.x, off2.y);

					col += GrabMain(uv + offd) + GrabMain(uv - offd);

					offd.x = -offd.x;

					col += GrabMain(uv + offd) + GrabMain(uv - offd);

					// Left And right
					offd = float2(off2.x, off.y);

					col += GrabMain(uv + offd) + GrabMain(uv - offd);

					offd.x = -offd.x;

					col += GrabMain(uv + offd) + GrabMain(uv - offd);


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