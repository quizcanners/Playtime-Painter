Shader "Playtime Painter/UI/ColorPicker/HUE_Radial"
{
	Properties{
		[PerRendererData]_MainTex("Mask (RGB)", 2D) = "white" {}
		[NoScaleOffset]_Arrow("Arrow", 2D) = "black" {}
		//	_SomeSlider("Reflectiveness or something", Range(0,1)) = 0
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

		SubShader{
			Pass{

				CGPROGRAM

				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _Arrow;
			//	float _SomeSlider;

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

					const float PI = 3.14159;

					const float PI2 = 3.14159 * 2;

					float2 uv = i.texcoord - 0.5;

					float angle = atan2(uv.x, uv.y);

					angle = saturate(max(angle, PI2 - max(0, -angle) - max(0, angle * 999999)) / PI2);

					float4 col = tex2D(_MainTex, i.texcoord);

					col.rgb = HUEtoColor(angle);

					float2 arrowUV = 0;
					 
					float diff = abs(frac(angle + 1.75) - (1-_Picker_HUV)); 

					arrowUV.x =  frac(min(diff, 1-diff ))*16;

					arrowUV.y = length(uv)*8-3;

					float2 inside = saturate((abs(float2(arrowUV.x,arrowUV.y-0.5) * 2) - 1) * 32);

					arrowUV.x += 0.5;

					float4 arrow = tex2D(_Arrow, arrowUV);

					arrow.a *= 1 - max(inside.x, inside.y);

					//col.rgb += max(0, (1 - length((arrowUV - 0.5) * 2)));

					col.rgb = arrow.rgb * arrow.a + col.rgb * (1 - arrow.a);

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

