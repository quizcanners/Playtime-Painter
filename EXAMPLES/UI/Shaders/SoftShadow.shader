Shader "Playtime Painter/UI/Soft Shadow" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "black" {}
		_NoiseMask("NoiseMask (RGB)", 2D) = "gray" {}
	}
	Category{
		Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"PixelPerfectUI" = "Position"
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
				#pragma multi_compile_instancing
				#pragma target 3.0

				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 precompute : TEXCOORD1;
					float3 offUV : TEXCOORD3;
					float4 projPos : TEXCOORD4;
					float4 color: COLOR;
				};

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = abs(o.texcoord.z)*10;
					o.projPos.xy = v.normal.xy;
					o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w = 1 / (1.0001 - o.texcoord.w);
					o.precompute.xy = 1 / (1.0001 - o.projPos.zw);
					o.precompute.z = (1 + o.texcoord.z * 16);


					o.offUV.xy = o.texcoord.xy - 0.5;
					o.offUV.z = saturate((o.color.a - 0.8) * 5);

					return o;
				}

				sampler2D _NoiseMask;

				float4 frag(v2f i) : COLOR{

					float4 _ProjTexPos = i.projPos;
					float _Edge = i.texcoord.z;
					float _Courners = i.texcoord.w;
					float deCourners = i.precompute.w;

					float4 noise = tex2Dlod(_NoiseMask, float4(i.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w)*32, 0, 0));


					float2 uv = abs(i.offUV.xy + noise.xy*0.002) * 2;

					uv = max(0, uv - _ProjTexPos.zw) * i.precompute.xy - _Courners;

					uv = max(0, uv) * deCourners;

			

					float clipp = max(0, 1 - dot(uv,uv));

					float4 col = i.color;

					col.a *= pow(clipp, _Edge + 1) *saturate((1 - clipp) * 10) * i.offUV.z;

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
