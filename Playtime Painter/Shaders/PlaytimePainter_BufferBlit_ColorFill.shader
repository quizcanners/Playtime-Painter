Shader "Playtime Painter/Buffer Blit/Color Fill" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
	}
	
	Category{
		Tags{ 
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
		}

		ColorMask RGBA
		Cull Back
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

				float4 _Color;

				struct v2f {
					float4 pos : POSITION;
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);   
					return o;
				}

				float4 frag(v2f i) : COLOR{
					return _Color;
				}
				ENDCG
			}
		}
	}
}