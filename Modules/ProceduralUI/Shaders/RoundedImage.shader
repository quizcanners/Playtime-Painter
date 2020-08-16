Shader "Playtime Painter/UI/Rounded/Image"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Sharpness", Range(0,1)) = 0.5
		[Toggle(TRIMMED)] trimmed("Trimmed Corners", Float) = 0
	}
	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Normal"
			"PerEdgeData" = "Linked"
		}

		ColorMask RGBA
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

				#pragma multi_compile_instancing
				#pragma shader_feature __ TRIMMED
				#pragma multi_compile ___ USE_NOISE_TEXTURE

				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 projPos : TEXCOORD1;
					float4 precompute : TEXCOORD2;
					float2 offUV : TEXCOORD3;
					float4 color: COLOR;
				};

				float _Edges;
				sampler2D _MainTex;
				float4 _MainTex_ST;
				sampler2D _Global_Noise_Lookup;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.texcoord.xy =		TRANSFORM_TEX(v.texcoord, _MainTex);
					o.color =			v.color;

					_Edges *=			v.color.a;

					o.texcoord.zw =		v.texcoord1.xy;
					o.texcoord.z =		4 - _Edges * 3;
					o.projPos.xy =		v.normal.xy;
					o.projPos.zw =		max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w =	1 / (1.0001 - o.texcoord.w);
					o.precompute.xy =	1 / (1.0001 - o.projPos.zw);
					o.precompute.z =	1 + _Edges * 32;

					o.offUV.xy =		(o.texcoord.xy - 0.5)*2;
			
					return o;
				}

				float4 frag(v2f o) : COLOR{

					float dx = abs(ddx(o.texcoord.x));
					float dy = abs(ddy(o.texcoord.y));
					float mip = (dx + dy) * 200;

					_Edges /= 1 + mip * mip; //LOD

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners = 1 - _Courners;
					float something =		o.precompute.w;
					float2 uv =				abs(o.offUV);

					float4 col = o.color * tex2Dlod(_MainTex, float4(o.texcoord.xy, 0, 0));

					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;

					uv = max(0, uv - _Courners) * something;

					#if TRIMMED
						float dist = (uv.x + uv.y); 
					#else
						float dist = dot(uv, uv);
					#endif

					float alpha = saturate(1 - dist);

					alpha = min(1, pow(alpha * o.precompute.z, o.texcoord.z));

					col.a *= alpha;


					#if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						#ifdef UNITY_COLORSPACE_GAMMA
							col.rgb += (noise.rgb - 0.5)*0.02*(3 - col.a * 2);
						#else
							col.rgb += (noise.rgb - 0.5)*0.0075*(3 - col.a * 2);
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
