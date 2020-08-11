Shader "Playtime Painter/Editor/Buffer Blit/Blend" {
	Properties{
		 _MainTex("Tex", 2D) = "white" {}
	}

	Category{
	
		ColorMask RGBA
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Back
		ZTest off
		ZWrite off


		SubShader{
			Pass{

				Tags{
				"RenderPipeline" = "UniversalPipeline"
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"LightMode" = "UniversalForward"
				}

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "PlaytimePainter_cg.cginc"

				sampler2D _MainTex;
				float _qcPp_CopyBlitAlpha;


				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
				};

				v2f vert(appdata_full_qc v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					return o;
				}

				float4 frag(v2f i) : COLOR{

					float4 col = tex2Dlod(_MainTex, float4(i.texcoord.xy, 0, 0));
					// TODO: Add avaraging like in the origina;

					col.a = _qcPp_CopyBlitAlpha;

					return col;
				}
				ENDCG
			}
		}

		SubShader{
			Pass{

				Tags{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"LightMode" = "ForwardBase"
			}

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "PlaytimePainter_cg.cginc"

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;
				float _qcPp_CopyBlitAlpha;


				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
				};

				v2f vert(appdata_full_qc v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					return o;
				}

				float4 frag(v2f i) : COLOR{

					float2 off = _MainTex_TexelSize.xy*1.5;

					float4 col = tex2Dlod(_MainTex, float4(i.texcoord.xy, 0, 0))

						+
						tex2Dlod(_MainTex, float4(i.texcoord.xy + off, 0, 0)) +
						tex2Dlod(_MainTex, float4(i.texcoord.xy - off, 0, 0));

					off.x = -off.x;

					col += tex2Dlod(_MainTex, float4(i.texcoord.xy + off, 0, 0)) +
						tex2Dlod(_MainTex, float4(i.texcoord.xy - off, 0, 0));

					col *= 0.2

						;

					col.a = _qcPp_CopyBlitAlpha;

					return col;
				}
				ENDCG
			}
		}
	}
}