Shader "Playtime Painter/Geometry/Unlit/TextureWithOverlay" {
	Properties{
		_MainTex("Main Texture (RGB)", 2D) = "white" {}
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
				float4 texcoord : 	TEXCOORD2;
			};

			uniform float4 _MainTex_ST;
			sampler2D _MainTex;
			uniform float4 _Overlay_ST;
			sampler2D _Overlay;

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoord.zw = TRANSFORM_TEX(v.texcoord, _Overlay);
				return o;
			}

			float4 frag(v2f o) : COLOR{
					
				float4 col = tex2D(_MainTex, o.texcoord.xy);
				float4 overlay = tex2D(_Overlay, o.texcoord.zw);

				col.rgb = col.rgb * (1 - overlay.a) + overlay.rgb*overlay.a;

				return col;
			}
			ENDCG
		}
	}
	Fallback "Legacy Shaders/Transparent/VertexLit"
}