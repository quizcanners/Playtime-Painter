Shader "Playtime Painter/UI/Primitives/Pixel Line" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		[KeywordEnum(Sides, Right, left, Inside)] _GRADS("Softening ", Float) = 0
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Position"
			"SpriteRole" = "Hide"
			"ShaderTip" = "If it disappears, try increasing the WIDTH of the line (or Height if line is vertical). "
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
				//#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma shader_feature _GRADS_SIDES _GRADS_RIGHT _GRADS_LEFT _GRADS_INSIDE 
				#pragma target 3.0

				struct v2f {
					float4 pos :		SV_POSITION;
					float2 courners	:	TEXCOORD1;
					float4 screenPos :	TEXCOORD2;
					float4 projPos :	TEXCOORD3;
					float4 color:		COLOR;
				};

				struct appdata_ui_qc
				{
					float4 vertex    : POSITION;  // The vertex position in model space.
					float2 texcoord  : TEXCOORD0; // The first UV coordinate.
					float2 texcoord1 : TEXCOORD1; // The second UV coordinate.
					float2 texcoord2 : TEXCOORD2; // The third UV coordinate.
					float4 color     : COLOR;     // Per-vertex color
				};


				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos =				UnityObjectToClipPos(v.vertex);
					o.screenPos =		ComputeScreenPos(o.pos);
					o.color =			v.color;
					//o.texcoord =		v.texcoord;

					o.projPos.xy =		v.texcoord2.xy;
					o.projPos.zw =		min(1, max(0, float2(v.texcoord1.x, -v.texcoord1.x))*2048);
								
					float2 tc			= v.texcoord.xy*o.projPos.zw;

					float sides =				(tc.x + tc.y);

					#if  !_GRADS_RIGHT && !_GRADS_LEFT
						sides = (sides - 0.5) * 2;
					#endif

					#if _GRADS_LEFT
						sides = 1 - sides;
					#endif

					o.projPos.zw *= _ScreenParams.yx;

					o.courners = float2(sides, v.texcoord1.y);

					return o;
				}


				float4 frag(v2f o) : COLOR{

					float4 _ProjTexPos =		o.projPos;
					float _Courners =			o.courners.y;

					float2 screenUV =			o.screenPos.xy / o.screenPos.w;

					float2 inPix =   (screenUV - _ProjTexPos.xy) * _ProjTexPos.wz;

					float sides = o.courners.x;

					#if _GRADS_INSIDE || _GRADS_SIDES
						sides = abs(sides);
					#endif

					#if  _GRADS_RIGHT || _GRADS_LEFT
						sides = pow(1.001 - sides, 1 + _Courners * 16)*saturate((1 - _Courners) * 32);
					#else
						sides = 1 - pow(sides, 1 + _Courners * 16)*saturate((1 - _Courners) * 32);
					#endif

					#if  _GRADS_INSIDE
						sides = 1 - sides;
					#endif
				
					o.color.a *= max(0,  round(1-abs(inPix.x + inPix.y)))*sides;
				
					return o.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
