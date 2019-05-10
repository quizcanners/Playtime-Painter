Shader "Playtime Painter/UI/Primitives/PixelLine" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Position"
			"SpriteRole" = "Hide"
			"ShaderTip" = "If it disappears, try increasing the WIDTH of the line (or Height if line is vertical). "
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
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				struct v2f {
					float4 pos :		SV_POSITION;
					//float2 texcoord :	TEXCOORD0;
					float2 courners	:	TEXCOORD0;
					float4 screenPos :	TEXCOORD1;
					float4 projPos :	TEXCOORD2;
					float4 color:		COLOR;
				};

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.screenPos =		ComputeScreenPos(o.pos);
					o.color =			v.color;
					//o.texcoord =		v.texcoord;

					o.projPos.xy =		v.normal.xy;
					o.projPos.zw =		min(1, max(0, float2(v.texcoord1.x, -v.texcoord1.x))*2048);
								
					float2 tc			= (v.texcoord.xy - 0.5)*o.projPos.zw;

					tc.x=				(tc.x + tc.y)*2;

					o.projPos.zw *= _ScreenParams.yx;

					o.courners = float2(tc.x, v.texcoord1.y);

					return o;
				}


				float4 frag(v2f o) : COLOR{

					float4 _ProjTexPos =		o.projPos;
					float _Courners =			o.courners.y;

					float2 screenUV =			o.screenPos.xy / o.screenPos.w;

					float2 inPix =   (screenUV - _ProjTexPos.xy) * _ProjTexPos.wz;

					float sides = abs(o.courners.x);

					sides = 1 - pow(sides, 1 + _Courners*16)*saturate((1- _Courners)*32);

					o.color.a *= max(0,  round(1-abs(inPix.x + inPix.y)))*sides;
				
					return o.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
