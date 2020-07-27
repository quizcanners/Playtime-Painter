Shader "Playtime Painter/UI/Rounded/Shadow" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Softness", Range(1,32)) = 2
		[Toggle(FILL)] trimmed("Fill inside", Float) = 0
	}
	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Hide"
			"PerEdgeData" = "Linked"
			
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				//#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma shader_feature __ FILL
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

				struct appdata_ui_qc
				{
					float4 vertex    : POSITION;  // The vertex position in model space.
					float2 texcoord  : TEXCOORD0; // The first UV coordinate.
					float2 texcoord1 : TEXCOORD1; // The second UV coordinate.
					float2 texcoord2 : TEXCOORD2; // The third UV coordinate.
					float4 color     : COLOR;     // Per-vertex color
				};

				v2f vert(appdata_ui_qc v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = abs(o.texcoord.z)*10;
					o.projPos.xy = v.texcoord2;
					o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w = 1 / (1.0001 - o.texcoord.w);
					o.precompute.xy = 1 / (1.0001 - o.projPos.zw);
					o.precompute.z = (1 + o.texcoord.z);

					o.offUV.xy = o.texcoord.xy - 0.5;
#if !FILL
					o.offUV.z = saturate((o.color.a - 0.8) * 5);
#else
					o.offUV.z = o.color.a;
#endif
					return o;
				}

				sampler2D _Global_Noise_Lookup;
				float _Edges;

				float4 frag(v2f o) : COLOR{

					float dx = abs(ddx(o.texcoord.x));
					float dy = abs(ddy(o.texcoord.y));
					float mip = (dx + dy) * 200;

					_Edges /= 1 + mip * mip; //LOD

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners =		o.precompute.w;

					float2 uv = abs(o.offUV.xy) * 2;
					
					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy - _Courners;

					uv = max(0, uv) * deCourners;

					float clipp = max(0, 1 - dot(uv,uv));

					float4 col = o.color;

					col.a *= pow(clipp, _Edges + 1) 
					#if !FILL
						*saturate((1 - clipp) * 10) 
					#endif
					* o.offUV.z;

					#if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						#ifdef UNITY_COLORSPACE_GAMMA
							col.rgb += (noise.rgb - 0.5)*0.02*(3 - col.a*2);
						#else
							col.rgb += (noise.rgb - 0.5)*0.0075*(3 - col.a*2);
						#endif
					#endif

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
