Shader "Playtime Painter/Editor/Brush/Double Buffered/Spread" {

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

				#pragma multi_compile  _FILL_TRANSPARENT  _FILL_NON_INKED 
				#pragma multi_compile  ____ BRUSH_3D  BRUSH_3D_TEXCOORD2
				#pragma multi_compile  ____ _qcPp_TARGET_TRANSPARENT_LAYER

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 worldPos : TEXCOORD1;
					float2 srcTexAspect : TEXCOORD2;
				};


				#if BRUSH_3D || BRUSH_3D_TEXCOORD2

				v2f vert(appdata_brush_qc v) {

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

				v2f vert(appdata_brush_qc v) {
					v2f o;

					o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					float2 suv = _qcPp_SourceTexture_TexelSize.zw;
					o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					return o;
				}

				#endif


				inline void DistAndInc(inout float2 dni, float2 uv, float3 brushColor) {
					//Distance and ink
					float4 col = tex2Dlod(_qcPp_DestBuffer, float4(uv, 0, 0));

					#if  _qcPp_TARGET_TRANSPARENT_LAYER
					float2 tmp;
					tmp.x = min(dni.x, length(col.rgb - brushColor.rgb));
					tmp.y = min(dni.y, length(col.rgb));

					dni = tmp * col.a + dni * (1 - col.a);

					//dni.y = 0;

					#else
					dni.x = min(dni.x, length(col.rgb - brushColor.rgb));
					dni.y = min(dni.y, col.r + col.b + col.g);
					#endif

				}

				float4 frag(v2f o) : COLOR{

					float blurAmount = _qcPp_brushForm.w;


					#if BRUSH_3D || BRUSH_3D_TEXCOORD2

						float a = prepareAlphaSphere(o.texcoord, o.worldPos.xyz);
						clip(a - 0.000001);
					#else

						float a = alphaFromUV(o.texcoord);
						clip(a);
					#endif

					float mask = getMaskedAlpha(o.texcoord.xy);

					float2 uv = o.texcoord.xy;

					float2 d = _qcPp_TargetTexture_TexelSize.xy;

					float2 dniDX = 3;

					DistAndInc(dniDX, uv + float2(-3 * d.x, 0), _qcPp_brushColor.rgb);
					DistAndInc(dniDX, uv + float2(-2 * d.x, 0), _qcPp_brushColor.rgb);
					DistAndInc(dniDX, uv + float2(-1 * d.x, 0), _qcPp_brushColor.rgb);
			
					float2 dniX = 3;

					DistAndInc(dniX, uv + float2(3 * d.x, 0), _qcPp_brushColor.rgb);
					DistAndInc(dniX, uv + float2(2 * d.x, 0), _qcPp_brushColor.rgb);
					DistAndInc(dniX, uv + float2(1 * d.x, 0), _qcPp_brushColor.rgb);

					float2 dniDY = 3;

					DistAndInc(dniDY, uv + float2(0, -3 * d.y), _qcPp_brushColor.rgb);
					DistAndInc(dniDY, uv + float2(0, -2 * d.y), _qcPp_brushColor.rgb);
					DistAndInc(dniDY, uv + float2(0, -1 * d.y), _qcPp_brushColor.rgb);

					float2 dniY = 3;

					DistAndInc(dniY, uv + float2(0, 3 * d.y), _qcPp_brushColor.rgb);
					DistAndInc(dniY, uv + float2(0, 2 * d.y), _qcPp_brushColor.rgb);
					DistAndInc(dniY, uv + float2(0, 1 * d.y), _qcPp_brushColor.rgb);

					float2 dni = 3;

					DistAndInc(dni, uv, _qcPp_brushColor.rgb);

					// X - Color distance
					// Y - BRIGHTNESS

					#define GETDIST(cidx,cidy) (cidx + max(0, 0.1 - cidy)*10)

					float dist = min(GETDIST(dniX.x, dniX.y), GETDIST(dniDX.x, dniDX.y));
					dist = min(dist, GETDIST(dniY.x, dniY.y));
					dist = min(dist, GETDIST(dniDY.x, dniDY.y));

					dist = max(dist*2, (0.1 - dni.y)*11);

					float alpha = min(1, saturate((a - 0.9975) * 400) + saturate(a * (1 - dist)*blurAmount));

					#if BLIT_MODE_COPY
						float4 src = tex2Dlod(_qcPp_SourceTexture, float4(o.texcoord.xy*o.srcTexAspect, 0, 0));
						alpha *= src.a;
						_qcPp_brushColor.rgb = SourceTextureByBrush(src.rgb);
					#endif


					#if  _qcPp_TARGET_TRANSPARENT_LAYER
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