Shader "Playtime Painter/UI/Effects/Sdf Shape"{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Cutoff("Mask Cutoff", Range(0,0.99)) = 0.5
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
		[Toggle(SHADOW)] _Shadow("Add Shadow", Float) = 0
		[Toggle(SUBTRACT)] _Subtract("Subtract", Float) = 0
		[Toggle(GRADIENT)] _Gradient("Gradient", Float) = 0
		//[Toggle(FADE_SHAKE)] _ShakeFade("Shake more when fading", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
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
			#pragma shader_feature _ SHADOW
			#pragma shader_feature _ SUBTRACT
			#pragma shader_feature _ GRADIENT

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				float4 screenPos : 	TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _Color;
			float4 _TextureSampleAdd;
			float4 _ClipRect;
			float4 _MainTex_ST;
			float _Cutoff;

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				OUT.screenPos = ComputeScreenPos(OUT.vertex);
				OUT.color = v.color;

				return OUT;
			}

			float4 frag(v2f IN) : SV_Target {

				float mask = tex2D(_MainTex, IN.texcoord).r;
				
				#if SUBTRACT
					mask = 1 - mask;
				#endif

				float delta = abs(fwidth(mask));

				delta = smoothstep(_Cutoff, _Cutoff + delta + 0.001, mask);

			
				
				#if GRADIENT
				float4 color = delta * (IN.color * IN.texcoord.y + _Color * (1- IN.texcoord.y) );
				#else
					float4 color = delta * IN.color * _Color;
				#endif
					
				#if SHADOW
					float shadow = tex2Dlod(_MainTex, float4(IN.texcoord + float2(0.04, 0.02) , 0, 2)).a;

					color.rgb *= color.a;
					color.a += shadow* 0.5 * (1 - color.a) * IN.color.a;
				#endif

				#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
				#endif

				return color;
			}
			ENDCG
		}
	}

  Fallback "Legacy Shaders/Transparent/VertexLit"
}