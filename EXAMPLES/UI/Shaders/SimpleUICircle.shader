Shader "Playtime Painter/UI/Primitives/Circle" {
	Properties{
		[PerRendererData]_MainTex("Mask (RGB)", 2D) = "white" {}
		_Edges("Softness", Range(1,32)) = 2
		[Toggle(FLIP_ALPHA)] flipA("Flip Alpha", Float) = 0
	}

	Category{
		Tags{
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
			"Queue" = "Transparent"
		}

		LOD 200
		ColorMask RGB
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#pragma multi_compile ___ FLIP_ALPHA

				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD2;
					float4 color : COLOR;
					float2 offUV : TEXCOORD3;
				};


				v2f vert(appdata_full v) {
					v2f o;

					o.texcoord = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					o.offUV.xy = o.texcoord.xy - 0.5;
					return o;
				}

				float _Edges;

				float4 frag(v2f o) : COLOR{

					float2 texUV = o.offUV;

					float2 uv = abs(o.offUV.xy) * 2;
					
					float clipp = max(0, 1 - dot(uv, uv));

					clipp = min(1, clipp*_Edges);

					#if FLIP_ALPHA
					clipp = 1 - clipp;
					#endif

					float4 col = o.color;

					col.a *= clipp;

					return col;
				}
				ENDCG
			}
		}
}
}
