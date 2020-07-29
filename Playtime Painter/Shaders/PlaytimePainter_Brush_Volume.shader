Shader "Playtime Painter/Editor/Brush/Volume" {
	Category{
		Tags{ "Queue" = "Transparent" }

		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM

				#include "PlaytimePainter_cg.cginc"
				#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_built_in.cginc"

				#pragma multi_compile  BLIT_MODE_ALPHABLEND  BLIT_MODE_ADD   BLIT_MODE_SUBTRACT   BLIT_MODE_COPY   BLIT_MODE_SAMPLE_DISPLACE

				#pragma vertex vert
				#pragma fragment frag

				float4 VOLUME_POSITION_N_SIZE_BRUSH;
				float4 VOLUME_H_SLICES_BRUSH;

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord.xy;
					return o;
				}

				float4 frag(v2f i) : COLOR{

					float3 worldPos = volumeUVtoWorld(i.texcoord.xy, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);

					#if BLIT_MODE_COPY
					_qcPp_brushColor = tex2Dlod(_qcPp_SourceTexture, float4(i.texcoord.xy, 0, 0));
					#endif

					#if BLIT_MODE_SAMPLE_DISPLACE
					_qcPp_brushColor.r = (_qcPp_brushSamplingDisplacement.x - i.texcoord.x - _qcPp_brushUvPosTo_Untiled.z) / 2 + 0.5;
					_qcPp_brushColor.g = (_qcPp_brushSamplingDisplacement.y - i.texcoord.y - _qcPp_brushUvPosTo_Untiled.w) / 2 + 0.5;
					#endif

					float alpha = prepareAlphaSphere(i.texcoord.xy, worldPos);

					//clip(alpha);

					#if BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY || BLIT_MODE_SAMPLE_DISPLACE
					return AlphaBlitOpaque(alpha, _qcPp_brushColor,  i.texcoord.xy);
					#endif

					#if BLIT_MODE_ADD
					return  addWithDestBuffer(alpha*0.04, _qcPp_brushColor,  i.texcoord.xy);
					#endif

					#if BLIT_MODE_SUBTRACT
					return  subtractFromDestBuffer(alpha*0.04, _qcPp_brushColor,  i.texcoord.xy);
					#endif

				}
				ENDCG
			}
		}
	}
}