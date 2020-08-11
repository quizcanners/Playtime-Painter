Shader "Playtime Painter/Editor/Buffer Blit/Multishade"
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

				 #pragma multi_compile BLIT_MODE_ALPHABLEND  BLIT_MODE_ADD  BLIT_MODE_SUBTRACT BLIT_MODE_COPY BLIT_MODE_SAMPLE_DISPLACE
				 #pragma multi_compile ____ _qcPp_TARGET_TRANSPARENT_LAYER

				 #pragma vertex vert
				 #pragma fragment frag

				 struct v2f {
					 float4 pos : POSITION;
					 float4 texcoord : TEXCOORD0;
					 //float4 worldPos : TEXCOORD1;
					 float2 srcTexAspect : TEXCOORD3;
				 };



				 v2f vert(appdata_full_qc v) {
					 v2f o;

					 //o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					 o.pos = UnityObjectToClipPos(v.vertex);
					 o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					 float2 suv = _qcPp_SourceTexture_TexelSize.zw;
					 o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					 return o;
				 }

				 float4 frag(v2f o) : COLOR{

					 float alpha = SampleAlphaBuffer(o.texcoord.xy);

					 #if BLIT_MODE_COPY

						float ignoreSrcAlpha = _qcPp_srcTextureUsage.w;

						 float4 src = tex2Dlod(_qcPp_SourceTexture, float4(o.texcoord.xy*o.srcTexAspect, 0, 0));
						 alpha *= ignoreSrcAlpha + src.a * (1- ignoreSrcAlpha);
						 _qcPp_brushColor.rgb = SourceTextureByBrush(src.rgb);
					 #endif

					 #if BLIT_MODE_SAMPLE_DISPLACE
						 _qcPp_brushColor.r = (_qcPp_brushSamplingDisplacement.x - o.texcoord.x - _qcPp_brushUvPosTo_Untiled.z) / 2 + 0.5;
						 _qcPp_brushColor.g = (_qcPp_brushSamplingDisplacement.y - o.texcoord.y - _qcPp_brushUvPosTo_Untiled.w) / 2 + 0.5;
					 #endif

					 #if BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY || BLIT_MODE_SAMPLE_DISPLACE 

					 #if (BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY) && _qcPp_TARGET_TRANSPARENT_LAYER
						 return AlphaBlitTransparent(alpha, _qcPp_brushColor,  o.texcoord.xy);
					 #else
						
						 return AlphaBlitOpaque(alpha, _qcPp_brushColor,  o.texcoord.xy);
					 #endif

					 #endif

					 #if BLIT_MODE_ADD
						 return  addWithDestBuffer(alpha, _qcPp_brushColor,  o.texcoord.xy);
					 #endif

					 #if BLIT_MODE_SUBTRACT
						 return  subtractFromDestBuffer(alpha, _qcPp_brushColor,  o.texcoord.xy);
					 #endif
				 }
				 ENDCG
			 }
		 }
	}
}
