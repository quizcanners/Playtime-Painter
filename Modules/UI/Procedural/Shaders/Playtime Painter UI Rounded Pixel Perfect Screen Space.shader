Shader "Playtime Painter/UI/Rounded/Pixel Perfect Screen Space" {
	Properties
	{
		[PerRendererData]
		_MainTex("Sprite Texture", 2D) = "white" {}
		_ColorOverlay("Color Overlay", Color) = (1,1,1,0)
		[KeywordEnum(Pixperfect, Fillscreen)]	_MODE("Mode", Float) = 0
		_Edges("Sharpness", Range(0,1)) = 0.5
		[Toggle(_SOFT_FADE)] softfade("Soft Fade", Float) = 0

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
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Tile"
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
			#pragma shader_feature __  _SOFT_FADE
			#pragma shader_feature _MODE_PIXPERFECT _MODE_FILLSCREEN 
			#pragma multi_compile ___ USE_NOISE_TEXTURE

			struct v2f
			{
				float4 vertex		: SV_POSITION;
				half4 color			: COLOR;
				float4 texcoord		: TEXCOORD0;
				float4 worldPosition: TEXCOORD1;
				float4 screenPos	: TEXCOORD2;
				float4 projPos		: TEXCOORD3;
				float4 precompute	: TEXCOORD4;
				float2 offUV		: TEXCOORD5;
#if _MODE_FILLSCREEN
				float2 stretch		: TEXCOORD6;
#endif
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;
			uniform float4 _ColorOverlay;
			uniform float4 _TextureSampleAdd;
			uniform float4 _ClipRect;
			uniform float4 _MainTex_TexelSize;
			uniform float _Edges;
			uniform sampler2D _Global_Noise_Lookup;

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
				o.color.rgb *= normalize(o.color.rgb)*1.75;
				

				#if _SOFT_FADE
					_Edges *= o.color.a * o.color.a;
				#endif

				o.texcoord.zw = v.texcoord1.xy;
				o.texcoord.z = 4 - _Edges * 3;
				o.projPos.xy = v.normal.xy;
				o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

				o.precompute.w = 1 / (1.0001 - o.texcoord.w);
				o.precompute.xy = 1 / (1.0001 - o.projPos.zw);
				o.precompute.z = 1 + _Edges * 32;

				o.offUV.xy = (o.texcoord.xy - 0.5) * 2;


				#if _MODE_FILLSCREEN

				float screenAspect = _ScreenParams.x * (_ScreenParams.w - 1);

				float texAspect = _MainTex_TexelSize.y * _MainTex_TexelSize.z;

				float2 aspectCorrection = float2(1, 1);

				if (screenAspect > texAspect)
					aspectCorrection.y = (texAspect / screenAspect);
				else
					aspectCorrection.x = (screenAspect / texAspect);

				o.stretch = aspectCorrection;
				#endif

				return o;
			}

			float4 frag(v2f o) : SV_Target {

				o.screenPos.xy /= o.screenPos.w;

#if _MODE_FILLSCREEN
				float2 fragCoord = (o.screenPos.xy - 0.5 ) * o.stretch.xy + 0.5;
#else
				float2 fragCoord = o.screenPos.xy * _ScreenParams.xy * _MainTex_TexelSize.xy;
#endif

				float4 color = tex2Dlod(_MainTex, float4(fragCoord ,0,0)) * o.color;

				color.rgb = _ColorOverlay.rgb * _ColorOverlay.a + color.rgb * (1 - _ColorOverlay.a);

				float4 _ProjTexPos = o.projPos;
				float _Courners = o.texcoord.w;
				float deCourners = 1 - _Courners;
				float something = o.precompute.w;
				float2 uv = abs(o.offUV);

				uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;

				uv = max(0, uv - _Courners) * something;

			#if TRIMMED
				float dist = (uv.x + uv.y);
			#else
				float dist = dot(uv, uv);
			#endif

				float alpha = saturate(1 - dist);

				alpha = saturate(pow(alpha * o.precompute.z, o.texcoord.z));

				color.a *= alpha;

				#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(o.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
				#endif

#if USE_NOISE_TEXTURE
				float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(fragCoord * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
#ifdef UNITY_COLORSPACE_GAMMA
				color.rgb += (noise.rgb - 0.5)*0.02;
#else
				color.rgb += (noise.rgb - 0.5)*0.0075;
#endif
#endif

				return color;
			}
			ENDCG
		}
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"
}