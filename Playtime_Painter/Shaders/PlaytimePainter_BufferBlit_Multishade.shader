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
				 #pragma multi_compile ____ TARGET_TRANSPARENT_LAYER

				 #pragma vertex vert
				 #pragma fragment frag

				 struct v2f {
					 float4 pos : POSITION;
					 float4 texcoord : TEXCOORD0;
					 //float4 worldPos : TEXCOORD1;
					 float2 srcTexAspect : TEXCOORD3;
				 };



				 v2f vert(appdata_full v) {
					 v2f o;

					 //o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					 o.pos = UnityObjectToClipPos(v.vertex);
					 o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					 float2 suv = _SourceTexture_TexelSize.zw;
					 o.srcTexAspect = max(1, float2(suv.y / suv.x, suv.x / suv.y));

					 return o;
				 }

				 float4 frag(v2f o) : COLOR{

					 // Brush Types

					// float2 off = _pp_AlphaBuffer_TexelSize.xy;


					//#define GRABPIXEL(ker) tex2Dlod(_pp_AlphaBuffer, float4(o.texcoord.xy + ker * off ,0,0)).r


					 float alpha = SampleAlphaBuffer(o.texcoord.xy);

					/*	 max (
							 GRABPIXEL(float2(0, 0)),
							 max(
								 max(
									max(GRABPIXEL(float2(-1, 0)), GRABPIXEL(float2(0, -1))),
									max(GRABPIXEL(float2(1, 0)), GRABPIXEL(float2(0, 1)))
								 ),
								 max(
									 max(GRABPIXEL(float2(-1, -1)), GRABPIXEL(float2(1, 1))),
									 max(GRABPIXEL(float2(-1, 1)), GRABPIXEL(float2(1, -1)))
								 )
							 )
						 );  

					 alpha = min(alpha, _pp_AlphaBufferCfg.x);*/

					 #if BLIT_MODE_COPY
						 float4 src = tex2Dlod(_SourceTexture, float4(o.texcoord.xy*o.srcTexAspect, 0, 0));
						 alpha *= src.a;
						 _brushColor.rgb = SourceTextureByBrush(src.rgb);
					 #endif

					 #if BLIT_MODE_SAMPLE_DISPLACE
						 _brushColor.r = (_brushSamplingDisplacement.x - o.texcoord.x - _brushPointedUV_Untiled.z) / 2 + 0.5;
						 _brushColor.g = (_brushSamplingDisplacement.y - o.texcoord.y - _brushPointedUV_Untiled.w) / 2 + 0.5;
					 #endif

					 #if BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY || BLIT_MODE_SAMPLE_DISPLACE 

					 #if (BLIT_MODE_ALPHABLEND || BLIT_MODE_COPY) && TARGET_TRANSPARENT_LAYER
						 return AlphaBlitTransparent(alpha, _brushColor,  o.texcoord.xy);
					 #else
						
						 return AlphaBlitOpaque(alpha, _brushColor,  o.texcoord.xy);
					 #endif

					 #endif

					 #if BLIT_MODE_ADD
						 return  addWithDestBuffer(alpha, _brushColor,  o.texcoord.xy);
					 #endif

					 #if BLIT_MODE_SUBTRACT
						 return  subtractFromDestBuffer(alpha, _brushColor,  o.texcoord.xy);
					 #endif
				 }
				 ENDCG
			 }
		 }
	}
}
