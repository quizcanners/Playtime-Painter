Shader "Playtime Painter/UI/Effects/ScreenSpaceFill Masked" {
	Properties
	{
		[PerRendererData]
		_MainTex("Mask Texture", 2D) = "white" {}
		_FillTex("Fill Texture", 2D) = "white" {}
		_ColorOverlay("Color Overlay", Color) = (1,1,1,0)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
		}

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct v2f
			{
				float4 vertex		: SV_POSITION;
				half4 color			: COLOR;
				float2 texcoord		: TEXCOORD0;
				float4 worldPosition: TEXCOORD1;
				float4 screenPos	: TEXCOORD2;
				float2 stretch		: TEXCOORD3;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;
			uniform sampler2D _FillTex;
			uniform float4 _ClipRect;
			//uniform float4 _MainTex_TexelSize;
			uniform float4 _FillTex_TexelSize;
			uniform float4 _ColorOverlay;

			v2f vert(appdata_full v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				o.texcoord.xy = v.texcoord;
				o.screenPos = ComputeScreenPos(o.vertex);
				o.color = v.color;

				float screenAspect = _ScreenParams.x * (_ScreenParams.w - 1);

				float texAspect = _FillTex_TexelSize.y * _FillTex_TexelSize.z;

				float2 aspectCorrection = float2(1, 1);

				if (screenAspect > texAspect)
					aspectCorrection.y = (texAspect / screenAspect);
				else
					aspectCorrection.x = (screenAspect / texAspect);

				o.stretch = aspectCorrection;


				return o;
			}

			float4 frag(v2f o) : SV_Target {

				float mask = tex2Dlod(_MainTex, float4(o.texcoord.xy ,0,0)).a;

				o.screenPos.xy /= o.screenPos.w;

				float2 fragCoord = (o.screenPos.xy - 0.5 ) * o.stretch.xy + 0.5;

				float4 color = tex2Dlod(_FillTex, float4(fragCoord ,0,0)) * o.color;

				color.a *= mask;

				#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(o.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
				#endif

				color.rgb = _ColorOverlay.rgb * _ColorOverlay.a + color.rgb * (1 - _ColorOverlay.a);

				return color;
			}
			ENDCG
		}
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"
}