Shader "Playtime Painter/UI/PixelPerfectSampling_MeshData" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_OutlineGradient("Outline Gradient", 2D) = "black" {}
		_Edges("Sharpness", Range(0,1)) = 0.5
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
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
					float4 pos :		SV_POSITION;
					float4 texcoord :	TEXCOORD0;
					float4 screenPos :	TEXCOORD1;
					float4 projPos :	TEXCOORD2;
					float4 precompute : TEXCOORD3;
					float4 precompute2 : TEXCOORD4;
					float4 offUV : TEXCOORD5;
					float4 color: COLOR;
			
				};

				sampler2D _MainTex;
				sampler2D _OutlineGradient;
				float4 _MainTex_TexelSize;
				float _Edges;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.texcoord.xy =		v.texcoord.xy;
					o.screenPos =		ComputeScreenPos(o.pos);
					o.color =			v.color;

					o.texcoord.zw =		v.texcoord1.xy; 
					o.texcoord.z = _Edges; //abs(o.texcoord.z);

					o.projPos.xy =		v.normal.xy; 
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


				float4 frag(v2f i) : COLOR{

					float4 _ProjTexPos =	i.projPos;
					float _Courners =		i.texcoord.w;
					float deCourners =		i.precompute.w;
					float2 uv =				abs(i.offUV) * 2;

					float2 screenUV =		i.screenPos.xy / i.screenPos.w;
					float2 inPix =			(screenUV - _ProjTexPos.xy)*_ScreenParams.xy;
					float2 texUV =			inPix * _MainTex_TexelSize.xy + i.offUV.zw;

					float4 col = tex2Dlod(_MainTex, float4(texUV + 0.5,0,0)); 
	
					uv = max(0, uv - _ProjTexPos.zw) * i.precompute.xy - _Courners;
			
					uv = max(0, uv) * deCourners;

					float clipp = max(0, 1 - dot(uv,uv));

					float4 outline = tex2Dlod(_OutlineGradient, float4(0, clipp,0,0));

					outline.a *= min(1,(1 - clipp)*16);

					clipp = min(1, pow(clipp * i.precompute.z, i.precompute2.x));

					col.rgb *= i.color.rgb;

					col.rgb = col.rgb*(1 - outline.a) + outline.rgb* (outline.a);

					col.a = max(col.a, outline.a);

					col.a *= clipp * i.color.a;


					return col;
				}
			ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
