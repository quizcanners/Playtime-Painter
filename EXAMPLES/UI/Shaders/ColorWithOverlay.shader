Shader "Playtime Painter/UI/ColorWithOverlay"
{
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "black" {}
		[NoScaleOffset]_Overlay("Overlay (RGB)", 2D) = "black" {}
	
	}
	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				struct v2f {
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD2;
					float4 color: COLOR;
				};

				sampler2D _MainTex;
				sampler2D _Overlay;
				

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					return o;
				}


				float4 frag(v2f i) : COLOR{
					float4 col = tex2D(_MainTex, i.texcoord);

					col.rgb *= i.color.rgb;

					float4 overlay = tex2D(_Overlay, i.texcoord);

					float alpha = min(1, overlay.a / (col.a + 0.0001));

					col.rgb = overlay.rgb * (alpha)+col.rgb * (1 - alpha);

					col.a = max(col.a, overlay.a)*i.color.a;

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
