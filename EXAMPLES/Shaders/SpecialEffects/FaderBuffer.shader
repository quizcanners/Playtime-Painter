Shader "Playtime Painter/Effects/FaderBuffer" {
	Properties{
		_Speed("FadeSpeed", Range(0.001,0.2)) = 0
	}

	Category{
		Tags{ 
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
			"Queue" = "Overlay+10"
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
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc" 
				#include "Lighting.cginc"
				#include "AutoLight.cginc"

				float _Speed;

				struct v2f {
					float4 pos : POSITION;
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					return o;
				}

				float4 frag(v2f i) : COLOR{
					float4 col = float4(0,0,0,_Speed);
					return col;
				}
				ENDCG
			
			}
		}
	}
}
