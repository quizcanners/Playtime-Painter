Shader "Playtime Painter/Editor/Brush/DoubleBuffer_Projector" {

	Category{

		Tags{ "Queue" = "Transparent"}

		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM

				#include "PlaytimePainter_cg.cginc"

				#pragma multi_compile  BRUSH_3D    BRUSH_3D_TEXCOORD2
				#pragma multi_compile  ____ _qcPp_TARGET_TRANSPARENT_LAYER
				#pragma multi_compile ___  _qcPp_USE_DEPTH_FOR_PROJECTOR

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 worldPos : TEXCOORD1;
					float4 shadowCoords : TEXCOORD2;
					float2 srcTexAspect : TEXCOORD3;
				};

				v2f vert(appdata_brush_qc v) {

					v2f o;
					float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0f));

					float t = _Time.w * 50;

					float2 jitter = _qcPp_AlphaBufferCfg.y * _qcPp_TargetTexture_TexelSize.xy * float2(sin(t), cos(t*1.3));

					o.worldPos = worldPos;

					#if BRUSH_3D_TEXCOORD2
					v.texcoord.xy = v.texcoord1.xy;
					#endif

					float2 suv = _qcPp_SourceTexture_TexelSize.zw;
					o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					float atY = floor(v.texcoord.z / _qcPp_brushAtlasSectionAndRows.z);
					float atX = v.texcoord.z - atY * _qcPp_brushAtlasSectionAndRows.z;
					v.texcoord.xy = (float2(atX, atY) + v.texcoord.xy) / _qcPp_brushAtlasSectionAndRows.z
						* _qcPp_brushAtlasSectionAndRows.w + v.texcoord.xy * (1 - _qcPp_brushAtlasSectionAndRows.w);

					worldPos.xyz = _qcPp_RTcamPosition.xyz;
					worldPos.z += 100;
					worldPos.xy += (v.texcoord.xy*_qcPp_brushEditedUVoffset.xy + _qcPp_brushEditedUVoffset.zw - 0.5 + jitter) * 256;

					v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz, v.vertex.w));

					o.pos = UnityObjectToClipPos(v.vertex);

					o.texcoord.xy = ComputeScreenPos(o.pos);

					o.texcoord.zw = o.texcoord.xy - 0.5;

					o.shadowCoords = mul(pp_ProjectorMatrix, o.worldPos);

					return o;
				}

				float4 frag(v2f o) : COLOR{

						o.shadowCoords.xy /= o.shadowCoords.w;

						float alpha = prepareAlphaSphere(o.shadowCoords.xy, o.worldPos.xyz);

						alpha *= ProjectorSquareAlpha(o.shadowCoords);
					
						float2 pUv = (o.shadowCoords.xy + 1) * 0.5;

						#if _qcPp_USE_DEPTH_FOR_PROJECTOR
						alpha *= ProjectorDepthDifference(o.shadowCoords, o.worldPos, pUv);
						#endif

						pUv *= o.srcTexAspect;

					
		
						float4 src = tex2Dlod(_qcPp_SourceTexture, float4(pUv, 0, 0));

						alpha *= src.a * BrushClamp(pUv);

						clip(alpha - 0.000001);

						_qcPp_brushColor.rgb = SourceTextureByBrush(src.rgb);

						// DEBUG
						//return o.shadowCoords;

					#if _qcPp_TARGET_TRANSPARENT_LAYER
						return AlphaBlitTransparent(alpha, _qcPp_brushColor,  o.texcoord.xy);
					#else
						return AlphaBlitOpaque(alpha, _qcPp_brushColor,  o.texcoord.xy);
					#endif
				}
				ENDCG
			}
		}
	}
}