Shader "Playtime Painter/Basic/ColorPicker_HUE"{
	Properties{
		_MainTex("Mask (RGB)", 2D) = "white" {}
		_Arrow("Arrow", 2D) = "black" {}
		_Value("HUE", Range(0,1)) = 1
	}

	Category{
		Tags{
			"Queue" = "AlphaTest"
			"IgnoreProjector" = "True"
		}

		Cull Off
		SubShader{

			Pass{

				CGPROGRAM

				#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

				#pragma vertex vert
				#pragma fragment frag
				//#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _Arrow;
				float _Value;

				struct v2f {
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD2;
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord.xy;
					return o;
				}

				float4 frag(v2f i) : COLOR{
		
					float4 col = tex2D(_MainTex, i.texcoord);

					col.rgb = HUEtoColor(i.texcoord.x);

					float2 arrowUV = i.texcoord;

					arrowUV.x = (i.texcoord.x - _Value) * 4;

					float2 inside = saturate((abs(arrowUV * 2) - 1)*32);

					arrowUV.x += 0.5;

					float4 arrow = tex2D(_Arrow, arrowUV);

					arrow.a *= 1- max(inside.x, inside.y);
	
					col = arrow * arrow.a + col * (1 - arrow.a);

					return col;
				}
				ENDCG

			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}

}

