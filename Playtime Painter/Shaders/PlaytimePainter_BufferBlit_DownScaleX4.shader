Shader "Playtime Painter/Buffer Blit/DownScaleX4" {
	Properties{
		 _MainTex("Tex", 2D) = "white" {}
	}
	
	Category{
		Tags{ 
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
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

				#include "PlaytimePainter_cg.cginc"

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;  
				};

				v2f vert(appdata_full v) {
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

					uv = (floor(uv*_MainTex_TexelSize.zw) + 0.5) * _MainTex_TexelSize.xy;

					float2 off = _MainTex_TexelSize.xy;

					float4 col = GrabMain(uv + off) + GrabMain(uv - off);

					off.x = -off.x;

					col += GrabMain(uv + off) + GrabMain(uv - off);

					col *= 0.25;

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