﻿Shader "Playtime Painter/UI/Rounded/Preserve Aspect"
{
	Properties{
		  [PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		  _OutlineGradient("Outline Gradient", 2D) = "black" {}
		  _Edges("Sharpness", Range(0,1)) = 0.5
		  [Toggle(_UNLINKED)] unlinked("Linked Corners", Float) = 0
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "AtlasedPosition"
			"SpriteRole" = "Tile"
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

				#pragma multi_compile ____  _UNLINKED 

				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _OutlineGradient;
				float4 _MainTex_TexelSize;
				float _Edges;

				struct appdata_ui_qc
				{
					float4 vertex    : POSITION;  // The vertex position in model space.
					float2 texcoord  : TEXCOORD0; // The first UV coordinate.
					float2 texcoord1 : TEXCOORD1; // The second UV coordinate.
					float2 texcoord2 : TEXCOORD2; // The third UV coordinate.
					float2 texcoord3 : TEXCOORD3; // The fourth UV coordinate.
					float4 color     : COLOR;     // Per-vertex color
				};

				struct v2f {
					float4 pos :			SV_POSITION;
					float4 texcoord :		TEXCOORD0;
					float4 projPos :		TEXCOORD1;
					float4 precompute :		TEXCOORD2;
					float4 precompute2 :	TEXCOORD3;
					float4 offUV :			TEXCOORD4;
					float4 color:			COLOR;

				};

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = _Edges;

					o.offUV.xy = o.texcoord.xy - 0.5;

					o.projPos.xy = v.texcoord2.xy;// +o.offUV.xy *((1 + v.texcoord1.xy)*0.5);

					o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w = 1 / (1.0001 - o.texcoord.w);
					o.precompute.xy = 1 / (1.0001 - o.projPos.zw);
					o.precompute.z = (1 + _Edges * 16);

				
					o.precompute2.x = 3 - _Edges * 2;
					o.precompute2.yzw = 0;

					
					o.offUV.zw = _MainTex_TexelSize.xy*0.5*(_MainTex_TexelSize.zw % 2);

					return o;
				}


				float4 frag(v2f o) : COLOR{

					float dx = abs(ddx(o.texcoord.x));
					float dy = abs(ddy(o.texcoord.y));
					float mip = (dx + dy) * 200;

					_Edges /= 1 + mip * mip; //LOD

					
					float _Courners = o.texcoord.w;
					float deCourners = o.precompute.w;
					float2 uv = abs(o.offUV) * 2;


					float4 col = tex2Dlod(_MainTex, float4(o.projPos.xy + o.offUV.xy * (1 + o.projPos.zw)*0.5, 0, 0));
					
					uv = max(0, uv - o.projPos.zw) * o.precompute.xy;

					float2 forFade = uv;

					uv = max(0, uv - _Courners) * deCourners;

					#if _UNLINKED
						forFade *= forFade;
						float clipp = max(0, 1 - max(max(forFade.x, forFade.y), dot(uv, uv)));
					#else 
						float clipp = max(0, 1 - dot(uv, uv));
					#endif

					float uvy = clipp * (1 + _Edges * 8);

					float4 outline = tex2Dlod(_OutlineGradient, float4(0, uvy,0,0));

					outline.a *= saturate((1 - uvy) * 16);

					clipp = min(1, pow(clipp * o.precompute.z, o.precompute2.x));

					col.rgb *= o.color.rgb;

					col.rgb = col.rgb*(1 - outline.a) + outline.rgb* (outline.a);

					#if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						#ifdef UNITY_COLORSPACE_GAMMA
							col.rgb += (noise.rgb - 0.5)*0.02*(3 - col.a * 2);
						#else
							col.rgb += (noise.rgb - 0.5)*0.0075*(3 - col.a * 2);
						#endif
					#endif

					col.a = max(col.a, outline.a);

					col.a *= clipp * o.color.a;

					return col;
				}
			ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
		}
			  CustomEditor "PlaytimePainter.PixelPerfectMaterialDrawer"
}
