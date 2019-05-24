Shader "Playtime Painter/Basic/TransparentOverrideColor"
{
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Color("_Color Override", Color) = (1,1,1,1)
	
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
					#pragma multi_compile_fwdbase

					struct v2f {
						float4 pos : SV_POSITION;
						float2 texcoord : TEXCOORD2;
						float4 color: COLOR;
					};

					sampler2D _MainTex;
					float4 _Color;

					v2f vert(appdata_full v) {
						v2f o;
						UNITY_SETUP_INSTANCE_ID(v);
					
						o.pos = UnityObjectToClipPos(v.vertex);
						o.texcoord.xy = v.texcoord.xy;

						return o;
					}

					float4 frag(v2f i) : COLOR{
						_Color.a = tex2D(_MainTex, i.texcoord).a;
						return _Color;
					}
					ENDCG
				}
			}
			Fallback "Legacy Shaders/Transparent/VertexLit"
		}
}
