Shader "Playtime Painter/UI/Rounded/Shadow" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Softness", Range(1,32)) = 2
	}
	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Hide"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile ___ USE_NOISE_TEXTURE
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
					o.precompute.z = (1 + o.texcoord.z);

					o.offUV.xy = o.texcoord.xy - 0.5;
					o.offUV.z = saturate((o.color.a - 0.8) * 5);

					return o;
				}

				sampler2D _Global_Noise_Lookup;
				float _Edges;

				float4 frag(v2f o) : COLOR{

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners =		o.precompute.w;

					float2 uv = abs(o.offUV.xy) * 2;
					
					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy - _Courners;

					uv = max(0, uv) * deCourners;

					float clipp = max(0, 1 - dot(uv,uv));

					float4 col = o.color;

					col.a *= pow(clipp, _Edges + 1) *saturate((1 - clipp) * 10) * o.offUV.z;

					#if USE_NOISE_TEXTURE

					float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));

					col.rgb += (noise.rgb - 0.5)*0.0075;

					#endif

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
