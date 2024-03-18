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


				float3 volumeUVtoWorld(float2 uv, float4 VOLUME_POSITION_N_SIZE, float4 VOLUME_H_SLICES) 
				{
					// H Slices:
					//hSlices, w * 0.5f, 1f / w, 1f / hSlices

					float hy = floor(uv.y*VOLUME_H_SLICES.x);
					float hx = floor(uv.x*VOLUME_H_SLICES.x);

					float2 xz = uv * VOLUME_H_SLICES.x;

					xz.x -= hx;
					xz.y -= hy;

					xz =  (xz*2.0 - 1.0) *VOLUME_H_SLICES.y;

					//xz *= VOLUME_H_SLICES.y*2;
					//xz -= VOLUME_H_SLICES.y;

					float h = hy * VOLUME_H_SLICES.x + hx;

					float3 bsPos = float3(xz.x, h, xz.y) * VOLUME_POSITION_N_SIZE.w;

					float3 worldPos = VOLUME_POSITION_N_SIZE.xyz + bsPos;

					return worldPos;
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