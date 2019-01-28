Shader "Playtime Painter/UI/RoundedBoxSingleColor"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Courners("Rounding Courners", Range(0,0.75)) = 0.5
		_Edge("Edge Sharpness", Range(0.1,1)) = 0.5
		_ProjTexPos("Screen Space Position", Vector) = (0,0,0,0)
	}
	Category{
		Tags{
			"Queue" = "Transparent-250"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ColorMask RGBA
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
					float4 texcoord : TEXCOORD2;
					float4 color: COLOR;
				};

			
				float _Courners;
				float _Edge;
				float4 _ProjTexPos;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					float2 scale = _ProjTexPos.zw;//_MainTex_TexelSize.zw;
					o.texcoord.zw = float2 (max(0, (scale.x - scale.y) / scale.x), max(0, (scale.y - scale.x) / scale.y));

					return o;
				}


				float4 frag(v2f i) : COLOR{

					
					float _Blur = (1 - i.color.a);
					float2 uv = abs(i.texcoord.xy - 0.5) * 2;
					uv = max(0, uv - i.texcoord.zw) / (1 - i.texcoord.zw) - _Courners;
					float deCourners = 1 - _Courners;
					uv = max(0, uv) / deCourners;
					uv *= uv;
					float clipp = max(0, (1 - uv.x - uv.y));

					float uvy = saturate(clipp * (4 - _Courners * 3)*(0.5 + _Edge * 0.5));

					i.color.a *= min(clipp* _Edge * (1 - _Blur)*deCourners * 30, 1);

					return i.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
