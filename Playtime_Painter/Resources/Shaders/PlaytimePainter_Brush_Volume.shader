Shader "Playtime Painter/Brush/Volume" {
	Category{
		Tags{ "Queue" = "Transparent" }

		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM

				#include "qc_Includes.cginc"
				#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

				#pragma multi_compile  BRUSH_NORMAL  BRUSH_ADD   BRUSH_SUBTRACT   BRUSH_COPY   BRUSH_SAMPLE_DISPLACE

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

					#if BRUSH_COPY
					_brushColor = tex2Dlod(_SourceTexture, float4(i.texcoord.xy, 0, 0));
					#endif

					#if BRUSH_SAMPLE_DISPLACE
					_brushColor.r = (_brushSamplingDisplacement.x - i.texcoord.x - _brushPointedUV_Untiled.z) / 2 + 0.5;
					_brushColor.g = (_brushSamplingDisplacement.y - i.texcoord.y - _brushPointedUV_Untiled.w) / 2 + 0.5;
					#endif

					float alpha = prepareAlphaSphere(i.texcoord.xy, worldPos);//positionToAlpha(worldPos);

					clip(alpha);

					#if BRUSH_NORMAL || BRUSH_COPY || BRUSH_SAMPLE_DISPLACE
					return AlphaBlitOpaque(alpha, _brushColor,  i.texcoord.xy);
					#endif

					#if BRUSH_ADD
					return  addWithDestBuffer(alpha*0.04, _brushColor,  i.texcoord.xy);
					#endif

					#if BRUSH_SUBTRACT
					return  subtractFromDestBuffer(alpha*0.04, _brushColor,  i.texcoord.xy);
					#endif

				}
				ENDCG
			}
		}
	}
}