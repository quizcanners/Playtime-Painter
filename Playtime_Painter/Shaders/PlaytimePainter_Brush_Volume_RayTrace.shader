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

				float4 RAYTRACE_POINT_1_COLOR;
				float4 RAYTRACE_POINT_1_POSITION;
				float4 RAYTRACE_POINT_1_NORMAL;

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
 
				void ApplyStroke(float3 worldPos, float3 brushPos, float3 brushNormal, float3 brushCol, float deSize, inout float4 col) {
					float3 diff = worldPos - brushPos;
					float dist = length(diff);
					float preAlpha = saturate(deSize / (dist + 0.000001)) * saturate(dot(normalize(diff + brushNormal * deSize), brushNormal) * 2);
					float alpha = saturate((preAlpha - col.a * 0.9)*2);
					col.a = max(preAlpha, col.a);
					col.rgb = brushCol.rgb * alpha + col.rgb * (1 - alpha);
				}

				float4 frag(v2f o) : COLOR {

					float3 worldPos = volumeUVtoWorld(o.texcoord.xy, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);

					float4 col = tex2Dlod(_DestBuffer, float4(o.texcoord.xy, 0, 0));

					float deSize = VOLUME_POSITION_N_SIZE_BRUSH.w;

					float3 brushNormal = VOLUME_BRUSH_DYRECTION.xyz;

					ApplyStroke(worldPos.xyz, _brushWorldPosTo.xyz, brushNormal, _brushColor, deSize, col);

					/*float3 diff = worldPos - _brushWorldPosTo;
					float dist = length(diff);
					float preAlpha = saturate(1/(dist+0.000001)) * saturate(dot(normalize(diff + brushNormal * deSize), brushNormal) * 2);
					float alpha = saturate((preAlpha - col.a * 0.9) * 5);
					col.a = max(preAlpha, col.a);
					col.rgb = _brushColor.rgb * alpha + col.rgb * (1 - alpha);*/

					return  col;

				}
				ENDCG
			}
		}
	}
}