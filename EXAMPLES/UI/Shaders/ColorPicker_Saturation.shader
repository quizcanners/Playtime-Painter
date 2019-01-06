Shader "Playtime Painter/UI/ColorPicker_Contrast" {
	Properties{
		_MainTex("Mask (RGB)", 2D) = "white" {}
		_Circle("Circle", 2D) = "black" {}
	
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Blend SrcAlpha OneMinusSrcAlpha
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

					col.rgb = HUEtoColor(_Picker_HUV);

	

					col.rgb = i.texcoord.y + col.rgb*(1-i.texcoord.y);

					col.rgb *= i.texcoord.x*i.texcoord.x;

					float2 dist = (i.texcoord - float2(_Picker_Brightness, _Picker_Contrast))*8;

					float ca = max(0, 1-max(0, abs(dist) - 0.5) * 32);

				    float4 circle = tex2D(_Circle, dist+0.5);

					ca *=  circle.a;

					//col.rgb += saturate(1 - length(dist));

					col.rgb = col.rgb*(1 - ca) + circle.rgb*ca;

					return col;
				}
					ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

