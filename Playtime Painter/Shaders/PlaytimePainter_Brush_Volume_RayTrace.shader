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
				#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_built_in.cginc"

				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile ___  _SMOOTHING

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
 
				void ApplyStroke(float3 brushCol, float deSize, inout float4 col, float hardness, float forward, float dist){
					
					float speed = _qcPp_brushForm.x;
					float sharpness = _qcPp_maskDynamics.y;
					
					float calculatedAlpha = saturate(1 / (deSize*dist + 0.000001)) * forward;
					float useNewColor = saturate((calculatedAlpha - col.a*0.75)*(2 + sharpness)*speed);
					col.a = max(calculatedAlpha, col.a);
					col.rgb = brushCol.rgb * useNewColor + col.rgb * (1 - useNewColor);
				}

				float4 frag(v2f o) : COLOR {

					float sharpness = _qcPp_maskDynamics.y;
					float hardness = (1 - 0.1 / (1 + sharpness));

					float3 worldPos = volumeUVtoWorld(o.texcoord.xy, VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);


					float3 diff = worldPos - _qcPp_brushWorldPosTo; //;
					float dist = length(diff);

					float forward = saturate(dot(normalize(diff), VOLUME_BRUSH_DYRECTION.xyz) * 128);


					float4 col = tex2Dlod(_qcPp_DestBuffer, float4(o.texcoord.xy, 0, 0));

					float deSize = VOLUME_POSITION_N_SIZE_BRUSH.w;

					ApplyStroke(_qcPp_brushColor, deSize, col, hardness, forward, dist);



					#if _SMOOTHING
					const float offs = 1;

					float4 up = SampleVolume(_qcPp_DestBuffer, worldPos + float3(0, offs, 0), VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);
					float4 down = SampleVolume(_qcPp_DestBuffer, worldPos + float3(0, -offs, 0), VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);
					float4 left = SampleVolume(_qcPp_DestBuffer, worldPos + float3(offs, 0, 0), VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);
					float4 right = SampleVolume(_qcPp_DestBuffer, worldPos + float3(-offs, 0, 0), VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);
					float4 fwd = SampleVolume(_qcPp_DestBuffer, worldPos + float3(0, 0, offs), VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);
					float4 back = SampleVolume(_qcPp_DestBuffer, worldPos + float3(0, 0, -offs), VOLUME_POSITION_N_SIZE_BRUSH, VOLUME_H_SLICES_BRUSH);

					float all = up.a + down.a + left.a + right.a + fwd.a + back.a + 0.0001;

					float4 awg = up * up.a + down * down.a + left * left.a + right * right.a + fwd * fwd.a + back * back.a;

					awg /= all;

					float smoothing = VOLUME_BRUSH_DYRECTION.w * saturate((awg.a - col.a)*100* forward);

					col.rgb = awg.rgb *smoothing + col.rgb*(1 - smoothing);

					#endif

					return  col;

				}
				ENDCG
			}
		}
	}
}