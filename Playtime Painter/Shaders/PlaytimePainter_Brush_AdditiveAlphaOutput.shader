Shader "Playtime Painter/Editor/Brush/AdditiveAlphaOutput" {

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Blend SrcAlpha One
		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM

				#include "PlaytimePainter_cg.cginc"

				#pragma multi_compile  BRUSH_2D BRUSH_SQUARE BRUSH_3D BRUSH_3D_TEXCOORD2

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 worldPos : TEXCOORD1;
				};

				#if BRUSH_3D || BRUSH_3D_TEXCOORD2
				v2f vert(appdata_full v) {

					v2f o;
					float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.worldPos = worldPos;

					#if BRUSH_3D_TEXCOORD2
					v.texcoord.xy = v.texcoord2.xy;
					#endif

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

					return o;
				}

				#else

				v2f vert(appdata_full v) {
					v2f o;

					o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					return o;
				}
				#endif

				float4 frag(v2f o) : COLOR{

					#if BRUSH_3D || BRUSH_3D_TEXCOORD2
					float alpha = prepareAlphaSphere(o.texcoord, o.worldPos.xyz);
					#endif

					#if BRUSH_2D
					float alpha = prepareAlphaSmooth(o.texcoord);
					#endif

					#if BRUSH_SQUARE
					float alpha = prepareAlphaSquare(o.texcoord.xy);
					#endif

					return alpha;

				}
				ENDCG
			}
		}
	}
}
