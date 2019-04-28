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
 
				void ApplyStroke(float3 worldPos, float3 brushPos, float3 brushNormal, float3 brushCol, float deSize, inout float4 col, float hardness) {
					
					float speed = _brushForm.x;
					float sharpness = _maskDynamics.y;
					
					float3 diff = worldPos - brushPos;
					float dist = length(diff);
					float preAlpha = saturate(1 / (deSize*dist + 0.000001)) * saturate(dot(normalize(diff + brushNormal * deSize), brushNormal) * 2);
					float alpha = saturate((preAlpha - col.a * hardness)*(2 + sharpness*0.1)*speed);
					col.a = max(preAlpha, col.a*(1-0.005*alpha));
					col.rgb = brushCol.rgb * alpha + max(col.rgb, brushCol.rgb*preAlpha) * (1 - alpha);
				}

				float4 frag(v2f o) : COLOR {

					float sharpness = _maskDynamics.y;
					float hardness = (1 - 0.1 / (1 + sharpness));

					float3 worldPos = volumeUVtoWorld(o.texcoord.xy, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);

					float offs = 0.25;

					float4 up = SampleVolume(_DestBuffer, worldPos, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH, float3(0, offs, 0));
					float4 down = SampleVolume(_DestBuffer, worldPos, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH, float3(0, -offs, 0));
					float4 left = SampleVolume(_DestBuffer, worldPos, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH, float3(offs, 0, 0));
					float4 right = SampleVolume(_DestBuffer, worldPos, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH, float3(-offs, 0, 0));
					float4 fwd = SampleVolume(_DestBuffer, worldPos, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH, float3(0, 0, offs));
					float4 back = SampleVolume(_DestBuffer, worldPos, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH, float3(0, 0, -offs));

					float all = up.a + down.a + left.a + right.a + fwd.a + back.a + 0.0001;

					float4 awg = up*up.a + down*down.a + left*left.a + right*right.a + fwd*fwd.a + back*back.a;

					awg /= all;

					float4 col = tex2Dlod(_DestBuffer, float4(o.texcoord.xy, 0, 0));

					float deSize = VOLUME_POSITION_N_SIZE_BRUSH.w;

					float3 brushNormal = VOLUME_BRUSH_DYRECTION.xyz;

					ApplyStroke(worldPos.xyz, _brushWorldPosTo.xyz, brushNormal, _brushColor, deSize, col, hardness);

					float portion = 0.5 + (col.a - awg.a)*0.5;

					col.rgb = col.rgb*portion+ 
						//max(col.rgb, awg.rgb)
						awg.rgb
						*(1 - portion);

					return  col;

				}
				ENDCG
			}
		}
	}
}