﻿Shader "Playtime Painter/Editor/Brush/BlurN_Smudge" {

	Category{
		Tags{ "Queue" = "Transparent"}

		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM

				#include "PlaytimePainter cg.cginc"

				#pragma multi_compile  BRUSH_2D  BRUSH_3D   BRUSH_3D_TEXCOORD2  //BRUSH_DECAL
				#pragma multi_compile  BRUSH_BLUR  BRUSH_BLOOM//BLIT_MODE_ALPHABLEND BLIT_MODE_ADD BLIT_MODE_COPY 

				#pragma vertex vert
				#pragma fragment frag

				#if !(BRUSH_3D || BRUSH_3D_TEXCOORD2)  //BRUSH_2D //|| BRUSH_DECAL
				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;  
				};

				v2f vert(appdata_brush_qc v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);   
					o.texcoord = brushTexcoord (v.texcoord.xy, v.vertex);
					return o;
				}
				#endif

				#if BRUSH_3D  ||  BRUSH_3D_TEXCOORD2

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;  
					float3 worldPos : TEXCOORD1;
				};

				v2f vert(appdata_brush_qc v) {

					v2f o;
					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					
					float4 uv = v.texcoord;

					#if BRUSH_3D_TEXCOORD2
						uv.xy = v.texcoord1.xy;
					#endif

					// ATLASED CALCULATION
					float atY = floor(uv.z / _qcPp_brushAtlasSectionAndRows.z);
					float atX = uv.z - atY * _qcPp_brushAtlasSectionAndRows.z;
					uv.xy = (float2(atX, atY) + uv.xy) / _qcPp_brushAtlasSectionAndRows.z
						* _qcPp_brushAtlasSectionAndRows.w + uv.xy * (1 - _qcPp_brushAtlasSectionAndRows.w);

					o.worldPos = worldPos.xyz;

					float2 tmp;

					worldPos.xyz = _qcPp_RTcamPosition.xyz;
					worldPos.z+=100;
					worldPos.xy+= (uv.xy*_qcPp_brushEditedUVoffset.xy+_qcPp_brushEditedUVoffset.zw-0.5)*256;

					v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz,v.vertex.w));

					o.pos = UnityObjectToClipPos( v.vertex );

					o.texcoord.xy = ComputeScreenPos(o.pos);

					return o;
				}
				#endif
	
				float4 frag(v2f i) : COLOR{

					float2 uv = i.texcoord.xy;
					float2 d = _qcPp_TargetTexture_TexelSize.xy;


					float xker = d.x * _qcPp_brushForm.w;
					float yker = d.y * _qcPp_brushForm.w;

					#if UNITY_COLORSPACE_GAMMA



					/*_qcPp_brushColor = pow(tex2Dlod(_qcPp_DestBuffer, float4(uv.x, uv.y, 0, 0)), GAMMA_TO_LINEAR);
	  				_qcPp_brushColor += pow(tex2Dlod(_qcPp_DestBuffer, float4(uv.x + d.x*1.5, uv.y + d.y*0.5, 0, 0)), GAMMA_TO_LINEAR);
	  				_qcPp_brushColor += pow(tex2Dlod(_qcPp_DestBuffer, float4(uv.x - d.x*0.5, uv.y + d.y*1.5, 0, 0)), GAMMA_TO_LINEAR);
	  				_qcPp_brushColor += pow(tex2Dlod(_qcPp_DestBuffer, float4(uv.x - d.x*1.5, uv.y - d.y*0.5, 0, 0)), GAMMA_TO_LINEAR);
	  				_qcPp_brushColor += pow(tex2Dlod(_qcPp_DestBuffer, float4(uv.x + d.x*0.5, uv.y - d.y*1.5, 0, 0)), GAMMA_TO_LINEAR);

					_qcPp_brushColor *= 0.2;
	  				_qcPp_brushColor = pow(_qcPp_brushColor, LINEAR_TO_GAMMA);*/




					#define GRABPIXELX(weight,kernel) pow(tex2Dlod( _qcPp_DestBuffer, float4(uv + float2(kernel*xker, 0)  ,0,0)), GAMMA_TO_LINEAR) * weight

					#define GRABPIXELY(weight,kernel) pow(tex2Dlod( _qcPp_DestBuffer, float4(uv + float2(0, kernel*yker)  ,0,0)), GAMMA_TO_LINEAR) * weight

					#else 

					#define GRABPIXELX(weight,kernel) tex2Dlod( _qcPp_DestBuffer, float4(uv + float2(kernel*xker, 0)  ,0,0)) * weight

					#define GRABPIXELY(weight,kernel) tex2Dlod( _qcPp_DestBuffer, float4(uv + float2(0, kernel*yker)  ,0,0)) * weight

					#endif

				
					float4 sum = 0;

					sum += GRABPIXELX(0.05, -4.0);
					sum += GRABPIXELX(0.09, -3.0);
					sum += GRABPIXELX(0.12, -2.0);
					sum += GRABPIXELX(0.15, -1.0);
					sum += GRABPIXELX(0.18, 0.0);
					sum += GRABPIXELX(0.15, +1.0);
					sum += GRABPIXELX(0.12, +2.0);
					sum += GRABPIXELX(0.09, +3.0);
					sum += GRABPIXELX(0.05, +4.0);

				
				
					sum += GRABPIXELY(0.05, -4.0);
					sum += GRABPIXELY(0.09, -3.0);
					sum += GRABPIXELY(0.12, -2.0);
					sum += GRABPIXELY(0.15, -1.0);
					sum += GRABPIXELY(0.18, 0.0);
					sum += GRABPIXELY(0.15, +1.0);
					sum += GRABPIXELY(0.12, +2.0);
					sum += GRABPIXELY(0.09, +3.0);
					sum += GRABPIXELY(0.05, +4.0);

					sum *= 0.5;

				

					/*_qcPp_brushColor = tex2Dlod(_qcPp_DestBuffer, float4(uv.x, uv.y, 0, 0))
								+ tex2Dlod(_qcPp_DestBuffer, float4(uv.x+d.x*1.5, uv.y+d.y*0.5, 0, 0))
								+ tex2Dlod(_qcPp_DestBuffer, float4(uv.x-d.x*0.5, uv.y+d.y*1.5, 0, 0))
								+ tex2Dlod(_qcPp_DestBuffer, float4(uv.x-d.x*1.5, uv.y-d.y*0.5, 0, 0))
								+ tex2Dlod(_qcPp_DestBuffer, float4(uv.x+d.x*0.5, uv.y-d.y*1.5, 0, 0));*/
					//_qcPp_brushColor *= 0.2;

					#if UNITY_COLORSPACE_GAMMA
					_qcPp_brushColor = pow(sum, LINEAR_TO_GAMMA);
					#else
					_qcPp_brushColor = sum;
					#endif

					#if BRUSH_3D || BRUSH_3D_TEXCOORD2
					float alpha = prepareAlphaSphere (i.texcoord, i.worldPos);
					clip(alpha - 0.000001);
					#endif

					#if BRUSH_2D
					float alpha = prepareAlphaSmooth (i.texcoord);
					#endif

					#if BRUSH_BLUR
					return AlphaBlitOpaque (alpha, _qcPp_brushColor,  i.texcoord.xy);
					#endif

					#if BRUSH_BLOOM
					return addWithDestBuffer (alpha, _qcPp_brushColor*0.1,  i.texcoord.xy);
					#endif

				}
				ENDCG
			}
		}
	}
}