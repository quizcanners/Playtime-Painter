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
				#pragma multi_compile  BLIT_MODE_ALPHABLEND    BLIT_MODE_ADD   BLIT_MODE_SUBTRACT   BLIT_MODE_COPY   BLIT_MODE_SAMPLE_DISPLACE
				#pragma multi_compile  ____ _qcPp_TARGET_TRANSPARENT_LAYER

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 worldPos : TEXCOORD1;
					float2 srcTexAspect : TEXCOORD3;
				};

				#if BRUSH_3D || BRUSH_3D_TEXCOORD2
				v2f vert(appdata_full_qc v) {

					v2f o;

					float t = _Time.w * 50;

					float2 jitter = _qcPp_AlphaBufferCfg.y * _qcPp_TargetTexture_TexelSize.xy * float2(sin(t), cos(t*1.3));

					float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.worldPos = worldPos;

					#if BRUSH_3D_TEXCOORD2
					v.texcoord.xy = v.texcoord1.xy;
					#endif

					float2 suv = _qcPp_SourceTexture_TexelSize.zw;
					o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					// ATLASED CALCULATION
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

					return o;
				}


				#else

				v2f vert(appdata_full_qc v) {
					v2f o;


					o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);	

					float2 suv = _qcPp_SourceTexture_TexelSize.zw;
					o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					return o;
				}
				#endif

				float4 frag(v2f o) : COLOR{

					// Brush Types

					#if BRUSH_3D || BRUSH_3D_TEXCOORD2

						float alpha = prepareAlphaSphere(o.texcoord, o.worldPos.xyz);
		
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
						float4 dest = tex2Dlod(_qcPp_DestBuffer, float4(o.texcoord.xy, 0, 0));
						float alpha = saturate((Height - dest.a) * 16 * _DecalParameters.y - 0.01);
						float4 col = tex2Dlod(_qcPp_DestBuffer, float4(o.texcoord.xy, 0, 0));
						float changeColor = _DecalParameters.z;
						_qcPp_brushColor = overlay * overlay.a + (_qcPp_brushColor*changeColor + col * (1 - changeColor))*(1 - overlay.a);
						_qcPp_brushColor.a = Height;
					#endif

						// Brush Modes

					#if BLIT_MODE_COPY

						float ignoreSrcAlpha = _qcPp_srcTextureUsage.w;

						float4 src = tex2Dlod(_qcPp_SourceTexture, float4(o.texcoord.xy*o.srcTexAspect, 0, 0));
						alpha *= ignoreSrcAlpha + src.a*(1- ignoreSrcAlpha);
						_qcPp_brushColor.rgb = SourceTextureByBrush(src.rgb);
					#endif

					#if BLIT_MODE_SAMPLE_DISPLACE
						_qcPp_brushColor.r = (_qcPp_brushSamplingDisplacement.x - o.texcoord.x - _qcPp_brushUvPosTo_Untiled.z) / 2 + 0.5;
						_qcPp_brushColor.g = (_qcPp_brushSamplingDisplacement.y - o.texcoord.y - _qcPp_brushUvPosTo_Untiled.w) / 2 + 0.5;
					#endif

					#if BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY || BLIT_MODE_SAMPLE_DISPLACE 
						//return o.texcoord.x;
					#if (BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY) && _qcPp_TARGET_TRANSPARENT_LAYER
						return AlphaBlitTransparent(alpha, _qcPp_brushColor,  o.texcoord.xy);
					#else
						return AlphaBlitOpaque(alpha, _qcPp_brushColor,  o.texcoord.xy);
					#endif

					#endif

					#if BLIT_MODE_ADD
						return  addWithDestBuffer(alpha*0.04, _qcPp_brushColor,  o.texcoord.xy);
					#endif

					#if BLIT_MODE_SUBTRACT
						return  subtractFromDestBuffer(alpha*0.04, _qcPp_brushColor,  o.texcoord.xy);
					#endif

				}
				ENDCG
			}
		}
	}
}