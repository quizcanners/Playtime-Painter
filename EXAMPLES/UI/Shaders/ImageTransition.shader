Shader "Playtime Painter/UI/ImageTransition" {
	Properties{
		[PerRendererData]_MainTex("Mask (RGB)", 2D) = "white" {}
		[NoScaleOffset]_MainTex_Current("First Texture", 2D) = "black" {}
		[NoScaleOffset]_Next_MainTex("Next Texture", 2D) = "black" {}
		[NoScaleOffset]_Mask("Mask", 2D) = "white" {}
		_Transition("Transition", Range(0,1)) = 0
		[NoScaleOffset]_Overlay("Overlay", 2D) = "black" {}
		
	}

	Category{
		Tags{ 
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
			"Queue" = "Transparent"
		}

		LOD 200
		ColorMask RGBA
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing

				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				sampler2D _MainTex_Current;
				float4 _MainTex_Current_TexelSize;
				sampler2D _Next_MainTex;
				float4 _MainTex_Current_ST;
				sampler2D _Overlay;
				sampler2D _Mask;

				float _Transition;


				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD2;
					float4 color : COLOR;
				};


				v2f vert(appdata_full v) {
					v2f o;

					o.texcoord = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					return o;
				}


				float4 frag(v2f i) : COLOR{

					float2 texUV = i.texcoord;

					texUV -= 0.5;

					float len = length(texUV);

					texUV *= 1 + (saturate(1 - i.color.a));

					texUV += 0.5;

					float _Courners = saturate((i.color.a - 0.4) * 2)*0.9;
					float _Blur = saturate((1 - i.color.a));
					float4 overlay = tex2D(_Overlay, texUV);
		
					float4 mask = tex2D(_Mask, texUV);

					float4 col = tex2Dlod(_MainTex_Current, float4(texUV, 0, 0));
					float4 col2 = tex2Dlod(_Next_MainTex, float4(texUV, 0, 0));

					float rgbAlpha = col2.a*_Transition;
					rgbAlpha = saturate(rgbAlpha * 2 / (col.a + rgbAlpha));
					col.a = col2.a * _Transition + col.a * (1 - _Transition);
					col.rgb = col2.rgb * rgbAlpha + col.rgb * (1 - rgbAlpha);

					col.a *= mask.a;

					col = col * (1 - overlay.a) + overlay * overlay.a;

					col.a *= (1 - _Blur);

					return saturate(col);
				}
				ENDCG
			}
		}
	}
}
