Shader "Playtime Painter/UI/Primitives/Pixel Line" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		[KeywordEnum(Sides, Right, left, Inside)] _GRADS("Softening ", Float) = 0


		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

	}

	Category{

		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Position"
			"SpriteRole" = "Hide"
			"ShaderTip" = "If it disappears, try increasing the WIDTH of the line (or Height if line is vertical). "
			"PerEdgeData" = "Linked"
		}


		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		ColorMask[_ColorMask]
		Cull Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				//#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
				#pragma multi_compile_local _ UNITY_UI_ALPHACLIP
				#pragma shader_feature _GRADS_SIDES _GRADS_RIGHT _GRADS_LEFT _GRADS_INSIDE 
				#pragma target 2.0

				float4 _ClipRect;

			struct appdata_ui_qc
			{
				float4 vertex    : POSITION;  // The vertex position in model space.
				float2 texcoord  : TEXCOORD0; // The first UV coordinate.
				float2 texcoord1 : TEXCOORD1; // The second UV coordinate.
				float2 texcoord2 : TEXCOORD2; // The third UV coordinate.
				float4 color     : COLOR;     // Per-vertex color
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 pos :		SV_POSITION;
				float2 courners	:	TEXCOORD1;
				float4 screenPos :	TEXCOORD2;
				float4 projPos :	TEXCOORD3;
				float4 worldPosition : TEXCOORD4;
				float4 color:		COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
				
			};

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				o.worldPosition = v.vertex;
				o.pos =				UnityObjectToClipPos(v.vertex);
				o.screenPos =		ComputeScreenPos(o.pos);
				o.color =			v.color;
				//o.texcoord =		v.texcoord;

				o.projPos.xy =		round(v.texcoord2.xy * _ScreenParams.xy) *(_ScreenParams.zw - 1);
				o.projPos.zw =		saturate(float2(v.texcoord1.x, -v.texcoord1.x)*99999);
								
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
				
				float4 col = o.color;

				col.a *= max(0,  round(1-abs(inPix.x + inPix.y)))*sides;
				
				/*#ifdef UNITY_UI_CLIP_RECT
					col.a *= UnityGet2DClipping(o.worldPosition.xy, _ClipRect);
				#endif*/

				#ifdef UNITY_UI_ALPHACLIP
					clip(col.a - 0.001);
				#endif

				return col;
			}
			ENDCG
		}
	}
	Fallback "Legacy Shaders/Transparent/VertexLit"
}
}
