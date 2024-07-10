﻿Shader "Playtime Painter/Editor/Preview/Brush" {
	Properties {
		_qcPp_PreviewTex ("Base (RGB)", 2D) = "white" { }
		_qcPp_AtlasTextures("_Textures In Row _ Atlas", float) = 1
	}
	Category{
		

		ColorMask RGBA
		Cull off

		SubShader{

			Tags{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			}

			Blend SrcAlpha OneMinusSrcAlpha

			Pass{

				CGPROGRAM

				#include "PlaytimePainter cg.cginc"

				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile  PREVIEW_RGB PREVIEW_ALPHA  PREVIEW_SAMPLING_DISPLACEMENT
				#pragma multi_compile  BRUSH_2D  BRUSH_3D  BRUSH_3D_TEXCOORD2  BRUSH_SQUARE  BRUSH_DECAL
				#pragma multi_compile  BLIT_MODE_ALPHABLEND BLIT_MODE_ADD BLIT_MODE_SUBTRACT BLIT_MODE_COPY  BLIT_MODE_PROJECTION
				#pragma multi_compile  ___ _qcPp_UV_ATLASED
				#pragma multi_compile  ___ _qcPp_BRUSH_TEXCOORD_2
				#pragma multi_compile  ___ _qcPp_TARGET_TRANSPARENT_LAYER

				sampler2D _qcPp_PreviewTex;
				float _qcPp_AtlasTextures;
				float4 _qcPp_PreviewTex_ST;
				float4 _qcPp_PreviewTex_TexelSize;
	
				struct v2f {
				float4 pos : SV_POSITION;
				float2 texcoord : TEXCOORD0;  
				float4 worldPos : TEXCOORD1;

				#if _qcPp_UV_ATLASED
					float4 atlasedUV : TEXCOORD2;
				#endif

				#if BLIT_MODE_PROJECTION
					float4 shadowCoords : TEXCOORD3;
				#endif
					float2 srcTexAspect : TEXCOORD4;

				};

				inline float getLOD(float2 uv, float4 _TexelSize) {

					float2 px = _TexelSize.z * ddx(uv.x);
					float2 py = _TexelSize.w * ddy(uv.y);

					return (max(0, 0.5 * log2(max(dot(px, px), dot(py, py)))));
				}

				v2f vert(appdata_brush_qc v) {
					v2f o;
					o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0f));

					#if BLIT_MODE_PROJECTION
					o.shadowCoords = mul(pp_ProjectorMatrix, o.worldPos);
					#endif


					o.pos = UnityObjectToClipPos(v.vertex);   

					#if _qcPp_BRUSH_TEXCOORD_2
						v.texcoord.xy = v.texcoord1.xy;
					#endif

					float2 suv = _qcPp_SourceTexture_TexelSize.zw;

					o.srcTexAspect = max(1, float2(suv.y/suv.x, suv.x / suv.y));

					o.texcoord.xy = TRANSFORM_TEX_QC(v.texcoord.xy, _qcPp_PreviewTex);
					
					#if _qcPp_UV_ATLASED
						float atY = floor(v.texcoord.z / _qcPp_AtlasTextures);
						float atX = v.texcoord.z - atY*_qcPp_AtlasTextures;
						float edge = _qcPp_PreviewTex_TexelSize.x;
						o.atlasedUV.xy = float2(atX, atY) / _qcPp_AtlasTextures;			
						o.atlasedUV.z = edge;										
						o.atlasedUV.w = 1 / _qcPp_AtlasTextures;
					#endif

					return o;
				}

				float4 Mix(float4 a, float4 b, float t) {
					return a * (1 - t) + b * t;
				}

				// from http://www.java-gaming.org/index.php?topic=35123.0
				float4 cubic_Interpolation(float v) {
					float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
					float4 s = n * n * n;
					float x = s.x;
					float y = s.y - 4.0 * s.x;
					float z = s.z - 4.0 * s.y + 6.0 * s.x;
					float w = 6.0 - x - y - z;
					return float4(x, y, z, w) * (1.0 / 6.0);
				}

				float4 textureBicubic(float2 texCoords) {

					float2 texSize = _qcPp_PreviewTex_TexelSize.zw;//textureSize(sampler, 0);
					float2 invTexSize = _qcPp_PreviewTex_TexelSize.xy;///1.0 / texSize;

					texCoords = texCoords * texSize - 0.5;

					float2 fxy = texCoords % 1;

					texCoords -= fxy;

					float4 xcubic = cubic_Interpolation(fxy.x);
					float4 ycubic = cubic_Interpolation(fxy.y);

					float4 c = texCoords.xxyy + float2(-0.5, +1.5).xyxy;

					float4 s = float4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);

					float4 offset = c + float4(xcubic.yw, ycubic.yw) / s;

					offset *= invTexSize.xxyy;

					float4 sample0 = tex2Dlod(_qcPp_PreviewTex, float4(offset.xz, 0, 0));
					float4 sample1 = tex2Dlod(_qcPp_PreviewTex, float4(offset.yz, 0, 0));
					float4 sample2 = tex2Dlod(_qcPp_PreviewTex, float4(offset.xw, 0, 0));
					float4 sample3 = tex2Dlod(_qcPp_PreviewTex, float4(offset.yw, 0, 0));

					float sx = s.x / (s.x + s.y);
					float sy = s.z / (s.z + s.w);

					return Mix(	Mix(sample3, sample2, sx), Mix(sample1, sample0, sx), sy);
				}

			
				float4 Sample(float2 uv) {
					#if  !BRUSH_SQUARE 	
						return textureBicubic(uv);
					#else
						return tex2Dlod(_qcPp_PreviewTex, float4(uv, 0, 0));
					#endif
				}

				float4 frag(v2f o) : COLOR{

					float4 col = 0;
					float alpha = 1;
					float ignoreSrcAlpha = _qcPp_srcTextureUsage.w;

					float dist = length(o.worldPos.xyz - _WorldSpaceCameraPos.xyz);

					float srcAlpha = 1;

					#if BLIT_MODE_PROJECTION
					
						o.shadowCoords.xy /= o.shadowCoords.w;

						alpha = ProjectorSquareAlpha(o.shadowCoords);

						float2 pUv = (o.shadowCoords.xy + 1) * 0.5;

						pUv *= o.srcTexAspect;

						float4 src = tex2Dlod(_qcPp_SourceTexture, float4(pUv, 0, 0));

						alpha *= (ignoreSrcAlpha + src.a * (1- ignoreSrcAlpha)) * BrushClamp(pUv);
						float pr_shadow = alpha;

						float par = _qcPp_srcTextureUsage.x;
					
						_qcPp_brushColor.rgb = SourceTextureByBrush(src.rgb);
						srcAlpha = src.a;
					#endif

					#if _qcPp_UV_ATLASED
						float seam = (o.atlasedUV.z)*pow(2, (log2(dist)));
						float2 fractal = (frac(o.texcoord.xy)*(o.atlasedUV.w - seam) + seam*0.5);
						o.texcoord.xy = fractal + o.atlasedUV.xy;
					#endif

					#if BLIT_MODE_COPY
						float4 src = tex2D(_qcPp_SourceTexture, o.texcoord.xy*o.srcTexAspect);
						_qcPp_brushColor.rgb = SourceTextureByBrush(src.rgb);
						srcAlpha = src.a;
					#endif

					float4 tc = float4(o.texcoord.xy, 0, 0);

					#if BRUSH_SQUARE
						float2 perfTex = (floor(tc.xy*_qcPp_PreviewTex_TexelSize.zw) + 0.5) * _qcPp_PreviewTex_TexelSize.xy;
						float2 off = (tc.xy - perfTex);

						float n = max(4,30 - dist); 

						float2 offset = saturate((abs(off) * _qcPp_PreviewTex_TexelSize.zw)*(n*2+2) - n);

						off = off * offset;

						tc.xy = perfTex  + off;

						tc.zw = previewTexcoord(tc.xy);

						col = Sample(tc.xy);

						float2 off2 = tc.zw*tc.zw;

						float fromCenter = 0.5*sqrt(off2.x+off2.y);
					
						float lod = getLOD(tc.xy, _qcPp_PreviewTex_TexelSize);

						float border = (1-saturate(fromCenter)) * max(offset.x, offset.y) * max(0, 1- lod*16);

						col = col*(1-border) + (0.5 - col * 0.5)*border;

						_qcPp_brushUvPosTo.xy = (floor (_qcPp_brushUvPosTo.xy*_qcPp_PreviewTex_TexelSize.zw)+ 0.5) * _qcPp_PreviewTex_TexelSize.xy;

					#else
					
						tc.zw = previewTexcoord(o.texcoord.xy);

					#endif

			
					#if  !BRUSH_SQUARE 	
						alpha *= checkersFromWorldPosition(o.worldPos.xyz,dist); 

						col = Sample(tc.xy);
					#endif

					#if BRUSH_3D  || BRUSH_3D_TEXCOORD2
						alpha *= prepareAlphaSpherePreview (tc.xy, o.worldPos);
					#endif

					#if BRUSH_2D || BRUSH_SQUARE

						#if (!BRUSH_SQUARE)
							alpha *= prepareAlphaSmoothPreview (tc);
							float differentColor = min(0.5, (abs(col.g-_qcPp_brushColor.g)+abs(col.r-_qcPp_brushColor.r)+abs(col.b-_qcPp_brushColor.b))*8);
							_qcPp_brushColor = _qcPp_brushColor*(differentColor+0.5);
						#else
							alpha *= prepareAlphaSquarePreview(tc);
						#endif
					#endif


						//	return _qcPp_brushColor;

					#if BRUSH_DECAL
						float2 decalUV = (tc.xy - _qcPp_brushUvPosTo.xy)*256/_qcPp_brushForm.y;

	 					float sinX = sin ( _DecalParameters.x );
						float cosX = cos ( _DecalParameters.x );
						float sinY = sin ( _DecalParameters.x );
						float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);

						decalUV =  mul ( decalUV, rotationMatrix );
      	
						float Height = tex2D(_VolDecalHeight, decalUV +0.5).a;
						float4 overlay = tex2D(_VolDecalOverlay, decalUV +0.5);
						float difference = saturate((Height-col.a) * 8*_DecalParameters.y-0.01);

						float changeColor = _DecalParameters.z;

						_qcPp_brushColor = overlay*overlay.a + (changeColor * _qcPp_brushColor+ col* (1-changeColor))*(1-overlay.a);

						decalUV = max(0,(abs(decalUV)-0.5));
						alpha *= difference*saturate(1-(decalUV.x+decalUV.y)*999999);
		 
					#endif

					#if PREVIEW_SAMPLING_DISPLACEMENT
						float resX = (tc.x + (col.r - 0.5) * 2);
						float resY = (tc.y + (col.g - 0.5) * 2);

						float edge = abs(0.5-((resX*_qcPp_brushSamplingDisplacement.z) % 1)) + abs(0.5 - (resY*_qcPp_brushSamplingDisplacement.w) % 1);

						float distX = (resX - _qcPp_brushSamplingDisplacement.x);
						float distY = (resY - _qcPp_brushSamplingDisplacement.y);
						col.rgb = saturate(1 - sqrt(distX*distX + distY * distY)*8) + saturate(edge);
					#endif

					#if PREVIEW_ALPHA
						col = col*_qcPp_brushMask + 0.5*(1 - _qcPp_brushMask)+col.a*_qcPp_brushMask.a;
					#endif
	
					#if BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY || BLIT_MODE_PROJECTION

						#if _qcPp_TARGET_TRANSPARENT_LAYER
						
							col = AlphaBlitTransparentPreview(alpha, _qcPp_brushColor, tc.xy, col, srcAlpha);

							float showBG = _qcPp_srcTextureUsage.z * (1-col.a);

							col.a += showBG; 

							col.rgb = col.rgb * (1 - showBG) + tex2D(_qcPp_TransparentLayerUnderlay, tc.xy).rgb*showBG;

						#else
							col = AlphaBlitOpaquePreview(alpha, _qcPp_brushColor, tc.xy, col, srcAlpha);

							col.a = 1;
						#endif

						#if BLIT_MODE_PROJECTION

							float pa = (_qcPp_brushUvPosTo.w)*pr_shadow*0.8;

							col = col * (1-pa) + _qcPp_brushColor*(pa);
						#endif

					#elif BLIT_MODE_ADD
						col =  addWithDestBufferPreview (alpha*0.4, _qcPp_brushColor, tc.xy, col, srcAlpha);
					#elif BLIT_MODE_SUBTRACT
						col =  subtractFromDestBufferPreview (alpha*0.4, _qcPp_brushColor, tc.xy, col);
					#endif

					#if !_qcPp_TARGET_TRANSPARENT_LAYER
						col.a = 1;
					#endif

					
					return col;

				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
	}
}