Shader "Playtime Painter/UI/ColorPicker_Contrast" {
	Properties{
		_MainTex("Mask (RGB)", 2D) = "white" {}
		_Circle("Circle", 2D) = "black" {}
		_brght("Brightness", Range(0,1)) = 1
		_ctrst("Contrast", Range(0,1)) = 1
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
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _Circle;
				float _brght;
				float _ctrst;
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

					col.rgb = HUEtoColor(_Value);

					col.rgb = i.texcoord.y + col.rgb*(1-i.texcoord.y);

					col.rgb *= i.texcoord.x;

					return col;
				}
					ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

