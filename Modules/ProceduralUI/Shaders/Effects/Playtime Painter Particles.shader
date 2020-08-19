Shader "Playtime Painter/UI/Particles"
{
	Properties{
		[PerRendererData]_MainTex("Shape Mask (A)", 2D) = "white" {}
		_Brighter("Bright Mask (R)", 2D) = "white" {}
		_Darker("Dark Mask (R)", 2D) = "white" {}
		_Visibility("Visibility", Range(0.1,32)) = 2
		_GyroidScale("Gyroid cale", Range(0.1,128)) = 32

		[Toggle(CLEAR_LIGHT)] clearLights("Clear Lights", Float) = 0

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
	}

	SubShader{

		Tags{
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

		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		ColorMask[_ColorMask]
		Cull Off

		Blend One One

		Pass{

			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma shader_feature ___ CLEAR_LIGHT
			#pragma target 2.0

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 pos : 		SV_POSITION;
				float2 texcoord : 	TEXCOORD0;
				float4 screenPos : 	TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
				float2 stretch		: TEXCOORD3;
				float4 color: 		COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _Global_Water_Particles_Mask;
			uniform float4 _MainTex_ST;
			sampler2D _MainTex;
			sampler2D _Brighter;
			sampler2D _Darker; 
			float _Visibility;
			float _GyroidScale;

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				o.worldPosition = v.vertex;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.screenPos = ComputeScreenPos(o.pos);
				o.color = v.color;


				float screenAspect = _ScreenParams.x * (_ScreenParams.w - 1);

				const float texAspect = 1;// _MainTex_TexelSize.y * _MainTex_TexelSize.z;

				float2 aspectCorrection = float2(1, 1);

				if (screenAspect > texAspect)
					aspectCorrection.y = (texAspect / screenAspect);
				else
					aspectCorrection.x = (screenAspect / texAspect);

				o.stretch = aspectCorrection;

				return o;
			}

			inline float DarkBrightGradient(float2 screenUV) {

				half t = _Time.x;

				half val = t * 15 + screenUV.x * 3 + screenUV.y * 8;

				half2 offA = screenUV * 1.3;
				half2 offB = screenUV * 1.1;

				half portion = saturate((cos(val) + 1)*0.5);
				half dePortion = 1 - portion;

				half brighter = tex2Dlod(_Brighter, half4(offA - half2(t*0.07, 0), 0, 0)).r;
				half brighterB = tex2Dlod(_Brighter, half4(offB - half2(0, t*0.09), 0, 0)).r;

				half darker = tex2Dlod(_Darker, half4(offA*2.5 + half2(t*(0.13), 0), 0, 0)).r;
				half darkerB = tex2Dlod(_Darker, half4(offB * 4 + half2(0, t*0.1), 0, 0)).r;

				float result = ((darker + brighter) * portion + (darkerB + brighterB) * dePortion);

				return result;

			}


			float4 frag(v2f o) : COLOR{

				float2 screenPos = o.screenPos.xy / o.screenPos.w;

				float2 screenPosCorrected = screenPos * o.stretch.xy;

				

				float value =  tex2Dlod(_MainTex, float4(o.texcoord.xy,0,0)).a;

				float3 p = float3(screenPosCorrected* (1+float2(_SinTime.x, _CosTime.x)*0.4), _Time.x*0.4 + value*0.5/_GyroidScale) * _GyroidScale;
				float gyroid = dot(sin(p), cos(p.zxy));

				

				//value *= saturate(1 - abs(gyroid));

				#ifdef UNITY_UI_CLIP_RECT
					value *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
					clip(value - 0.001);
				#endif
				float4 col = o.color;

				col.a = 1;

			#if CLEAR_LIGHT
				float grad = 0.6;
			#else
	 		    float grad = DarkBrightGradient(screenPosCorrected);
			#endif

				//float shape = sin((screenPos.x + screenPos.y + _Time.x)*64) - cos(value * 3);

				

				col.rgb *= (smoothstep(0, 1, (1-abs(gyroid))) + 1) * grad  * value * _Visibility;

				float3 mix = col.gbr + col.brg;

				col.rgb += mix.rgb * mix.rgb*0.025;

				return col;
			}
		ENDCG
		}
	}
	Fallback "Legacy Shaders/Transparent/VertexLit"

}