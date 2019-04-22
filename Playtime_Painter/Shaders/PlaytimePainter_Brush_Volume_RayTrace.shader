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
				float4 VOLUME_BRUSH_DYRECTION;

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

					//VOLUME_BRUSH_DYRECTION

					//float dott = dot(VOLUME_BRUSH_DYRECTION.xyz, worldPos - );

					//TODO: Make correct offset to world position
					//TODO: Use distance as alpha, not actual brush alpha

					float3 diff = worldPos - _brushWorldPosTo;

					float dist = length(diff);

					float preAlpha = saturate(1/(dist+0.000001)) * saturate(dot(normalize(diff), VOLUME_BRUSH_DYRECTION.xyz) * 16);

					//float preAlpha = saturate(POINT_BRUSH_ALPHA_DIRECTED(worldPos + VOLUME_BRUSH_DYRECTION.xyz, VOLUME_BRUSH_DYRECTION.xyz)); //saturate(positionToAlpha(worldPos));
					
					float alpha = saturate((preAlpha - col.a * 0.9) * 5);

					col.a = max(preAlpha, col.a);

					col.rgb = _brushColor.rgb * alpha + col.rgb * (1 - alpha);

					return  col;

				}
				ENDCG
			}
		}
	}
}