Shader "Playtime Painter/Editor/Brush/Volume_RayTrace" {
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
				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

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

				float4 frag(v2f o) : COLOR{

					float3 worldPos = volumeUVtoWorld(o.texcoord.xy, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);

					float4 col = tex2Dlod(_DestBuffer, float4(o.texcoord.xy, 0, 0));

					float alpha = saturate(positionToAlpha(worldPos));
					
					

					alpha = saturate((pow(alpha,2) - max(0,col.a * 0.8)) * 5);

					col.a = max(alpha, col.a);

					alpha *= 0.5;

					col.rgb = _brushColor.rgb * alpha + col.rgb * (1 - alpha);

					//return 0;

					return  col; //max(0, col);

				}
				ENDCG
			}
		}
	}
}