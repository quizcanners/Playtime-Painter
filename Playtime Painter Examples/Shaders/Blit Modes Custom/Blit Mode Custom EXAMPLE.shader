Shader "Playtime Painter/Editor/Brush/Custom/EXAMPLE"{
	Properties{}

		Category{
			Tags{
				"Queue" = "Transparent"
				"RenderType" = "Transparent"
			}

			//Blend SrcAlpha OneMinusSrcAlpha   // <- For Single buffer shader Uncomment this
			ColorMask RGB
			Cull off
			ZTest off
			ZWrite off


			SubShader{
				Pass{

					CGPROGRAM

					#include "Assets\Tools\Playtime Painter\Shaders\PlaytimePainter_cg.cginc"

					#pragma vertex vert
					#pragma fragment frag

					struct v2f {
						float4 pos : POSITION;
						float4 texcoord : TEXCOORD0;
					};

					v2f vert(appdata_full v) {
						v2f o;
						o.pos = UnityObjectToClipPos(v.vertex);
						o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);
						return o;
					}
				
					float4 frag(v2f i) : COLOR
					{
						float2 uv = i.texcoord.xy; // Optional surce texture to copy color from or to use as a mask

						float4 srcTexture = tex2Dlod(_qcPp_SourceTexture, float4(uv * 4, 0, 0));

						float2 leakingUv = uv - (_qcPp_brushUvPosTo.xy - _qcPp_brushUvPosFrom.xy)* srcTexture.r; // This is for the Leaking effect

						float4 col = tex2Dlod(_qcPp_DestBuffer, float4(leakingUv, 0, 0)); // Target Texture - the texture you are painting on. Only for DOUBLE_BUFFERED painting

						float alpha = prepareAlphaSmooth(i.texcoord);	// using bush size and position gets brush alpha

						_qcPp_brushColor.rgb = col.rgb;//_qcPp_brushColor.rgb * 0.2;

						
						
						/* UNCOMMENT FOR SINGLE BUFFER:
						   _qcPp_brushColor.a = alpha;
							return  _qcPp_brushColor;
						*/

						return AlphaBlitOpaque(alpha, _qcPp_brushColor, uv);

						
					}
					ENDCG
				}
			}
	}
}
