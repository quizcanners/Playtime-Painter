Shader "Playtime Painter/Editor/Buffer Blit/Projector Brush"
{
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

				 #pragma multi_compile ____ _qcPp_TARGET_TRANSPARENT_LAYER

				 #pragma vertex vert
				 #pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
				};

				v2f vert(appdata_brush_qc v) {

					v2f o;
	
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					return o;
				}

				float4 frag(v2f o) : COLOR{

					float4 buff = SampleUV_AlphaBuffer(o.texcoord.xy);
					float2 uv = buff.rg;

					float4 src = tex2Dlod(_qcPp_SourceTexture, float4(uv, 0, 0));

					float ignoreSrcAlpha = _qcPp_srcTextureUsage.w;

					float alpha = min(1,buff.a)*(ignoreSrcAlpha + src.a * (1- ignoreSrcAlpha));

					_qcPp_brushColor.rgb = SourceTextureByBrush(src.rgb);

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
