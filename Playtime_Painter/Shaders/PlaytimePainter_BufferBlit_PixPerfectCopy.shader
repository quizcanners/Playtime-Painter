Shader "Playtime Painter/Buffer Blit/Pixel Perfect Copy" {
	Properties{
		 _MainTex("Tex", 2D) = "white" {}
	}
	
	Category{
		Tags{ 
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
		}

		//Blend SrcAlpha OneMinusSrcAlpha 
		ColorMask RGBA
		Cull Off
		ZTest off
		ZWrite off


		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;  
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);    // Position on the screen
					o.texcoord.xy = v.texcoord;

					return o;
				}

				 /*    x contains 1.0/width
				 y contains 1.0/height
				 z contains width
				 w contains height*/

				float4 frag(v2f i) : COLOR{
	
					float2 perfTex = (floor(i.texcoord.xy*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;

					float4 col = tex2Dlod(_MainTex, float4(perfTex, 0, 0));

					return col;

				}
				ENDCG
			}
		}
	}
}
