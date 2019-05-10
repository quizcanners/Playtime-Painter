Shader "Playtime Painter/Geometry/Unlit/VertexColorWithOverlay" {
	Properties{
		_Overlay("Overlay (RGB)", 2D) = "black" {}
	}

	SubShader{

		Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
		}

		ColorMask RGB

		Pass{

			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			struct v2f {
				float4 pos : 		SV_POSITION;
				float2 texcoord : 	TEXCOORD0;
				float4 color :		COLOR0;
			};

			uniform float4 _Overlay_ST;
			sampler2D _Overlay;

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord.xy = TRANSFORM_TEX(v.texcoord, _Overlay);
				o.color= v.color;
				return o;
			}

			float4 frag(v2f o) : COLOR{
					
				float4 col = o.color;
				float4 overlay = tex2D(_Overlay, o.texcoord.xy);

				col.rgb = col.rgb * (1 - overlay.a) + overlay.rgb*overlay.a;

				return col;
			}
			ENDCG
		}
	}
	Fallback "Legacy Shaders/Transparent/VertexLit"
}