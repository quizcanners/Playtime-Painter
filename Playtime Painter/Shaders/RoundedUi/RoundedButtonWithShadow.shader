Shader "Playtime Painter/UI/Rounded/ButtonWithShadow" {
	
	Properties{
		[PerRendererData] _MainTex("Albedo (RGB)", 2D) = "black" {}
		_Shadow("Shadow", Range(0,15)) = 0.5
		_Edges ("Button Sharpness", Range(0,1)) = 0.5
	}

	Category{
		
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
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
					float4 screenPos :	TEXCOORD5;
					float4 color: COLOR;
				};

				float _Shadow;
				float _Edges;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.texcoord.xy =		v.texcoord.xy;
					o.screenPos =		ComputeScreenPos(o.pos);
					o.color =			v.color;

					o.texcoord.zw =		v.texcoord1.xy;
					o.texcoord.z =		_Shadow;
					o.projPos.xy =		v.normal.xy;
					o.projPos.zw =		max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w =	1 / (1.0001 - o.texcoord.w);
					o.precompute.xy =	1 / (1.0001 - o.projPos.zw);
					o.precompute.z =	(1 + o.texcoord.z *  (16 - _Shadow * 15));

					o.offUV.xy =		(o.texcoord.xy - 0.5)*2;
					o.offUV.z =			saturate((o.color.a - 0.8) * 5);

					return o;
				}

				sampler2D _Global_Noise_Lookup;

				float4 frag(v2f o) : COLOR{

					float dx = abs(ddx(o.texcoord.x));
					float dy = abs(ddy(o.texcoord.y));
					float mip = (dx + dy) * 200;

					_Edges /= 1 + mip * mip; //LOD

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners =		o.precompute.w;

					float2 uv = abs(o.offUV.xy);

					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy - _Courners;

					uv = max(0, uv) * deCourners;

					float clipp = max(0, 1 - dot(uv,uv));

					float4 col = o.color;

					float button = pow(clipp, 1 + _Shadow);

					float inn = 1 + 10 * _Edges;

					float inner = min(1, button * 1024) * saturate((1 - (1 - clipp) * 10) * inn);

					col.rgb *= inner;
					
					float mtp = 0.25*(1 - button);

					#if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						#ifdef UNITY_COLORSPACE_GAMMA
							col.rgb += (noise.rgb - 0.5)*0.02*(3 - col.a * 2);
						#else
							col.rgb += (noise.rgb - 0.5)*0.0075*(3 - col.a * 2);
						#endif
					#endif

					col.a = inner + (1- inner)* button* (1+mtp);

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
