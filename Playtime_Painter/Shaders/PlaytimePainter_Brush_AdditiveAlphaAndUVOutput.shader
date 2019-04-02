Shader "Playtime Painter/Editor/Brush/AdditiveUV_Alpha" {

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Blend One Zero, SrcAlpha One 


		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM

				#include "PlaytimePainter_cg.cginc"

				#pragma multi_compile BRUSH_3D BRUSH_3D_TEXCOORD2

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 worldPos : TEXCOORD1;
					float4 shadowCoords : TEXCOORD2;
					float2 srcTexAspect : TEXCOORD3;
				};

				v2f vert(appdata_full v) {

					v2f o;
					float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.worldPos = worldPos;

					#if BRUSH_3D_TEXCOORD2
						v.texcoord.xy = v.texcoord2.xy;
					#endif

					float2 suv = _SourceTexture_TexelSize.zw;
					o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

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

					o.shadowCoords = mul(pp_ProjectorMatrix, o.worldPos);

					return o;
				}


				float4 frag(v2f o) : COLOR {

					o.shadowCoords.xy /= o.shadowCoords.w;

					float alpha = prepareAlphaSphere(o.shadowCoords.xy, o.worldPos.xyz);

					clip(alpha - 0.000001);

					alpha *= ProjectorSquareAlpha(o.shadowCoords);

					float2 pUv;
					alpha *= ProjectorDepthDifference(o.shadowCoords, o.worldPos, pUv);

					pUv *= o.srcTexAspect;

					alpha *= BrushClamp(pUv);

					clip(alpha - 0.01);

					return float4(pUv, 0, alpha);

				}
				ENDCG
			}
		}
	}
}