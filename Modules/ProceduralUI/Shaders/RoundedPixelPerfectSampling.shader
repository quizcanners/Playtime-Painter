Shader "Playtime Painter/UI/Rounded/Pixel Perfect" {
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
			"PixelPerfectUI" = "Position"
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
				#include "UnityUI.cginc"

				#pragma vertex vert
				#pragma fragment frag
			
				#pragma multi_compile ____  _UNLINKED 

				//#pragma multi_compile_fwdbase
				//#pragma multi_compile_instancing
				#pragma target 3.0

				struct v2f {
					float4 pos :			SV_POSITION;
					float4 texcoord :		TEXCOORD0;
					float4 screenPos :		TEXCOORD1;
					float4 projPos :		TEXCOORD2;
					float4 precompute :		TEXCOORD3;
					float4 precompute2 :	TEXCOORD4;
					float4 offUV :			TEXCOORD5;
					float4 color:			COLOR;
			
				};

				struct appdata_ui_qc
				{
					float4 vertex    : POSITION;  // The vertex position in model space.
					float2 texcoord  : TEXCOORD0; // The first UV coordinate.
					float2 texcoord1 : TEXCOORD1; // The second UV coordinate.
					float2 texcoord2 : TEXCOORD2; // The third UV coordinate.
					//float2 texcoord3 : TEXCOORD3; // The fourth UV coordinate.
					float4 color     : COLOR;     // Per-vertex color
				};


				sampler2D _MainTex;
				sampler2D _OutlineGradient;
				float4 _MainTex_TexelSize;
				float _Edges;

				v2f vert(appdata_ui_qc v) {
					v2f o;
					//UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.texcoord.xy =		v.texcoord.xy;
					o.screenPos =		ComputeScreenPos(o.pos);
					o.screenPos.xy *=	_ScreenParams.xy;
					o.color =			v.color;

					o.texcoord.zw =		v.texcoord1.xy; 
					o.texcoord.z =		_Edges; 

					o.projPos.xy =		floor(v.texcoord2.xy * _ScreenParams.xy);
					o.projPos.zw =		max(0, float2(v.texcoord1.x, -v.texcoord1.x));
			
					o.precompute.w =	1/( 1.0001 - o.texcoord.w);
					o.precompute.xy =	1/(1.0001 - o.projPos.zw);
					o.precompute.z =	(1 + _Edges * 16);

					o.precompute2 =		0;
					o.precompute2.x =	3 - _Edges * 2;
					
					o.offUV.xy =		o.texcoord.xy - 0.5;
					o.offUV.zw =		_MainTex_TexelSize.xy*0.5*(_MainTex_TexelSize.zw % 2);

					return o;
				}


				float4 frag(v2f o) : COLOR{

					float dx = abs(ddx(o.texcoord.x));
					float dy = abs(ddy(o.texcoord.y));
					float mip = (dx + dy) * 200;

					_Edges /= 1 + mip * mip; //LOD

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners =		o.precompute.w;
					float2 uv =				abs(o.offUV) * 2;

					float2 inPix =			o.screenPos.xy / o.screenPos.w - _ProjTexPos.xy;
					float2 texUV =			inPix * _MainTex_TexelSize.xy + o.offUV.zw;

					float4 col = tex2Dlod(_MainTex, float4(texUV + 0.5,0,0)); 
	
					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;
			
					float2 forFade = uv;

					uv = max(0, uv - _Courners) * deCourners;

					#if _UNLINKED
					forFade *= forFade;
					float clipp = max(0, 1 - max(max(forFade.x, forFade.y), dot(uv, uv)));
					#else 
					float clipp = max(0, 1 - dot(uv, uv));
					#endif

                    float uvy = clipp*(1+_Edges*8); 

					float4 outline = tex2Dlod(_OutlineGradient, float4(0, uvy,0,0));

					outline.a *= saturate((1 - uvy)*16);

					clipp = min(1, pow(clipp * o.precompute.z, o.precompute2.x));

					col.rgb *= o.color.rgb;

					col.rgb = col.rgb*(1 - outline.a) + outline.rgb* (outline.a);

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
