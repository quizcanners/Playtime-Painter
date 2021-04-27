Shader "Playtime Painter/Editor/Brush/Single Buffer/Volume" {
	Category{
		Tags{ "Queue" = "Transparent" }

		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off
		Blend SrcAlpha OneMinusSrcAlpha 

		SubShader{
			Pass{

				CGPROGRAM

				#include "Assets/The-Fire-Below/Common/Shaders/quizcanners_cg.cginc"
				#include "PlaytimePainter cg.cginc"

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

					float alpha = prepareAlphaSphere(i.texcoord.xy, worldPos);

					_qcPp_brushColor.a = alpha;

					return _qcPp_brushColor;
				}
				ENDCG
			}
		}
	}
}