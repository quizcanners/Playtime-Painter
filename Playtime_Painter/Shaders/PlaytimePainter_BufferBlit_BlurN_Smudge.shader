Shader "Playtime Painter/Editor/Buffer Blit/BlurN_Smudge" {

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

				#pragma multi_compile  BRUSH_BLUR  BRUSH_BLOOM
				 #pragma multi_compile ____ TARGET_TRANSPARENT_LAYER

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;  
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);   
					o.texcoord = brushTexcoord (v.texcoord.xy, v.vertex);
					return o;
				}

				float4 frag(v2f o) : COLOR{

					float2 uv = o.texcoord.xy;
					float2 d = _DestBuffer_TexelSize.xy*_brushForm.w;

					#if UNITY_COLORSPACE_GAMMA

					#define GRABPIXELX(weight,kernel) pow(tex2Dlod( _DestBuffer, float4(uv + float2(kernel*xker, 0)  ,0,0)), GAMMA_TO_LINEAR) * weight

					#define GRABPIXELY(weight,kernel) pow(tex2Dlod( _DestBuffer, float4(uv + float2(0, kernel*yker)  ,0,0)), GAMMA_TO_LINEAR) * weight

					#else 

					#define GRABPIXELX(weight,kernel) tex2Dlod( _DestBuffer, float4(uv + float2(kernel*xker, 0)  ,0,0)) * weight

					#define GRABPIXELY(weight,kernel) tex2Dlod( _DestBuffer, float4(uv + float2(0, kernel*yker)  ,0,0)) * weight

					#endif


					float4 sum = 0;

					float xker = 0.0001*_brushForm.w;
				
					sum += GRABPIXELX(0.05, -4.0);
					sum += GRABPIXELX(0.09, -3.0);
					sum += GRABPIXELX(0.12, -2.0);
					sum += GRABPIXELX(0.15, -1.0);
					sum += GRABPIXELX(0.18, 0.0);
					sum += GRABPIXELX(0.15, +1.0);
					sum += GRABPIXELX(0.12, +2.0);
					sum += GRABPIXELX(0.09, +3.0);
					sum += GRABPIXELX(0.05, +4.0);

					float yker = 0.0001*_brushForm.w;

					sum += GRABPIXELY(0.05, -4.0);
					sum += GRABPIXELY(0.09, -3.0);
					sum += GRABPIXELY(0.12, -2.0);
					sum += GRABPIXELY(0.15, -1.0);
					sum += GRABPIXELY(0.18, 0.0);
					sum += GRABPIXELY(0.15, +1.0);
					sum += GRABPIXELY(0.12, +2.0);
					sum += GRABPIXELY(0.09, +3.0);
					sum += GRABPIXELY(0.05, +4.0);

					sum *= 0.5;

					#if UNITY_COLORSPACE_GAMMA
					_brushColor = pow(sum, LINEAR_TO_GAMMA);
					#else
					_brushColor = sum;
					#endif

					float alpha = SampleAlphaBuffer(o.texcoord.xy);

					#if BRUSH_BLUR
					return AlphaBlitOpaque (alpha, _brushColor,  o.texcoord.xy);
					#endif

					#if BRUSH_BLOOM
					return addWithDestBuffer (alpha, _brushColor*0.1,  o.texcoord.xy);
					#endif

				}
				ENDCG
			}
		}
	}
}