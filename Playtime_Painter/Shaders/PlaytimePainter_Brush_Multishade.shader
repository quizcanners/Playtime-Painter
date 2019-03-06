Shader "Playtime Painter/Editor/Brush/DoubleBuffer" {

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

				#pragma multi_compile  BRUSH_SQUARE    BRUSH_2D    BRUSH_3D    BRUSH_3D_TEXCOORD2  BRUSH_DECAL
				#pragma multi_compile  BLIT_MODE_ALPHABLEND    BLIT_MODE_ADD   BLIT_MODE_SUBTRACT   BLIT_MODE_COPY   BLIT_MODE_SAMPLE_DISPLACE  BLIT_MODE_PROJECTION
				#pragma multi_compile  ____ TARGET_TRANSPARENT_LAYER

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 worldPos : TEXCOORD1;
					#if BLIT_MODE_PROJECTION
					float4 shadowCoords : TEXCOORD2;
					#endif
					float2 srcTexAspect : TEXCOORD3;
				};



				#if BRUSH_3D || BRUSH_3D_TEXCOORD2
				v2f vert(appdata_full v) {

					v2f o;
					float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.worldPos = worldPos;

					#if BRUSH_3D_TEXCOORD2
					v.texcoord.xy = v.texcoord2.xy;
					#endif

					float2 suv = _SourceTexture_TexelSize.zw;
					o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					// ATLASED CALCULATION
					float atY = floor(v.texcoord.z / _brushAtlasSectionAndRows.z);
					float atX = v.texcoord.z - atY * _brushAtlasSectionAndRows.z;
					v.texcoord.xy = (float2(atX, atY) + v.texcoord.xy) / _brushAtlasSectionAndRows.z
						* _brushAtlasSectionAndRows.w + v.texcoord.xy * (1 - _brushAtlasSectionAndRows.w);

					worldPos.xyz = _RTcamPosition.xyz;
					worldPos.z += 100;
					worldPos.xy += (v.texcoord.xy*_brushEditedUVoffset.xy + _brushEditedUVoffset.zw - 0.5) * 256;

					v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz, v.vertex.w));

					o.pos = UnityObjectToClipPos(v.vertex);

					o.texcoord.xy = ComputeScreenPos(o.pos);

					o.texcoord.zw = o.texcoord.xy - 0.5;

					#if BLIT_MODE_PROJECTION
					o.shadowCoords = mul(pp_ProjectorMatrix, o.worldPos);
					#endif

					return o;
				}


				#else

				v2f vert(appdata_full v) {
					v2f o;

					o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);	


					float2 suv = _SourceTexture_TexelSize.zw;
					o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					return o;
				}
				#endif

				float4 frag(v2f o) : COLOR{

					// Brush Types

					#if BLIT_MODE_PROJECTION
						o.shadowCoords.xy /= o.shadowCoords.w;
					#endif

					#if BRUSH_3D || BRUSH_3D_TEXCOORD2

						#if BLIT_MODE_PROJECTION
							float alpha = prepareAlphaSphere(o.shadowCoords.xy, o.worldPos.xyz);
						#else
							float alpha = prepareAlphaSphere(o.texcoord, o.worldPos.xyz);
						#endif

						clip(alpha - 0.000001);
					#endif

					#if BRUSH_2D
						float alpha = prepareAlphaSmooth(o.texcoord);
					#endif

					#if BRUSH_SQUARE
						float alpha = prepareAlphaSquare(o.texcoord.xy);
					#endif

					#if BRUSH_DECAL
						float2 decalUV = o.texcoord.zw + 0.5;
						float Height = tex2D(_VolDecalHeight, decalUV).a;
						float4 overlay = tex2D(_VolDecalOverlay, decalUV);
						float4 dest = tex2Dlod(_DestBuffer, float4(o.texcoord.xy, 0, 0));
						float alpha = saturate((Height - dest.a) * 16 * _DecalParameters.y - 0.01);
						float4 col = tex2Dlod(_DestBuffer, float4(o.texcoord.xy, 0, 0));
						float changeColor = _DecalParameters.z;
						_brushColor = overlay * overlay.a + (_brushColor*changeColor + col * (1 - changeColor))*(1 - overlay.a);
						_brushColor.a = Height;
					#endif

						// Brush Modes

					#if BLIT_MODE_COPY
						float4 src = tex2Dlod(_SourceTexture, float4(o.texcoord.xy*o.srcTexAspect, 0, 0));
						alpha *= src.a;
						_brushColor.rgb = SourceTextureByBrush(src.rgb);
					#endif

					#if BLIT_MODE_SAMPLE_DISPLACE
						_brushColor.r = (_brushSamplingDisplacement.x - o.texcoord.x - _brushPointedUV_Untiled.z) / 2 + 0.5;
						_brushColor.g = (_brushSamplingDisplacement.y - o.texcoord.y - _brushPointedUV_Untiled.w) / 2 + 0.5;
					#endif

					#if BLIT_MODE_PROJECTION

						alpha *= ProjectorSquareAlpha(o.shadowCoords);

						float2 pUv;
						alpha *= ProjectorDepthDifference(o.shadowCoords, o.worldPos, pUv);

						pUv *= o.srcTexAspect;

						float4 src = tex2Dlod(_SourceTexture, float4(pUv, 0, 0));

						alpha *= src.a * BrushClamp(pUv);

						_brushColor.rgb = SourceTextureByBrush(src.rgb);

					#endif

					#if BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY || BLIT_MODE_SAMPLE_DISPLACE || BLIT_MODE_PROJECTION

					#if (BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY) && TARGET_TRANSPARENT_LAYER
						return AlphaBlitTransparent(alpha, _brushColor,  o.texcoord.xy);
					#else
						return AlphaBlitOpaque(alpha, _brushColor,  o.texcoord.xy);
					#endif

					#endif

					#if BLIT_MODE_ADD
						return  addWithDestBuffer(alpha*0.04, _brushColor,  o.texcoord.xy);
					#endif

					#if BLIT_MODE_SUBTRACT
						return  subtractFromDestBuffer(alpha*0.04, _brushColor,  o.texcoord.xy);
					#endif

				}
				ENDCG
			}
		}
	}
}