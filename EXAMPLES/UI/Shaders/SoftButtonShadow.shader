Shader "MilkLab/UI/SoftButtonShadow" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Courners("Rounding Courners", Range(0,0.75)) = 0.5
		_Edge("Edge Softness", Range(1,10)) = 1
		_ProjTexPos("Screen Space Position", Vector) = (0,0,0,0)
	}
	Category{
		Tags{
			"Queue" = "Background"
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
					//float3 viewDir: TEXCOORD4;
					//float4 screenPos : TEXCOORD5;
					float4 color: COLOR;
				};

			
				float _Courners;
				float _Edge;
				float4 _ProjTexPos;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					//o.viewDir.xyz = WorldSpaceViewDir(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					//o.screenPos = ComputeScreenPos(o.pos);
					o.color = v.color;

					float2 scale = _ProjTexPos.zw;//_MainTex_TexelSize.zw;
					o.texcoord.zw = float2 (max(0, (scale.x - scale.y) / scale.x), max(0, (scale.y - scale.x) / scale.y));

					return o;
				}


				float4 frag(v2f i) : COLOR{

					// Rounded Courners
					float _Blur = (1 - i.color.a);
					float2 uv = abs(i.texcoord.xy - 0.5) * 2;
					uv = max(0, uv - i.texcoord.zw) / (1 - i.texcoord.zw) - _Courners;
					float deCourners = 1 - _Courners;
					uv = max(0, uv) / deCourners;
					uv *= uv;
					float clipp = max(0, (1 - uv.x - uv.y));

					float4 col = i.color;

					col.a *= pow(clipp, _Edge);

					/*float upEdge = 0.5*(1 + _Edge);

					float uvy = saturate(clipp * (4 - _Courners * 3)* upEdge);

					float outA = saturate((1 - uvy) * 4 * upEdge);

					

					col.a *= outA;

					col.a *= min(clipp * upEdge *(1 - _Blur) * deCourners * 30, 1); */

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
