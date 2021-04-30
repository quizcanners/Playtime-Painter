Shader "Playtime Painter/UI/Effects/Shaky"{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
		[Toggle(SHADOW)] _Shadow("Add Shadow", Float) = 0
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
			//#pragma shader_feature ___ FADE_SHAKE

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

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				OUT.screenPos = ComputeScreenPos(OUT.vertex);

				float2 deCenter = OUT.texcoord - 0.5;

				float2 sp = OUT.screenPos.xy;

				float2 off = float2(sin((_Time.x * 3 + sp.y*5 - sp.x)*10), cos((_Time.x * 4.2 + sp.x*3 + sp.y)*10));

				float shake = max(0,(v.color.a-0.75)*4) * v.color.a;

				/*#if FADE_SHAKE
				off *= (1 + (1-v.color.a)*10);
				#endif*/

				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition + float4((off + deCenter * abs(off)*0.5)* 2 * shake,0,0));

				OUT.color = v.color * _Color;

				return OUT;
			}

		  half4 frag(v2f IN) : SV_Target {

	
					half4 color = (tex2Dlod(_MainTex, half4(IN.texcoord,0, 0)) + _TextureSampleAdd) * IN.color;

					#if SHADOW
						half shadow = tex2Dlod(_MainTex, half4(IN.texcoord + half2(0.04, 0.02) , 0, 2)).a;

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