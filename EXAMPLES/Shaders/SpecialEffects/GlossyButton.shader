Shader "Playtime Painter/Basic/GlossyButton" {
	Properties{
		[PerRendererData]_MainTex("Albedo", 2D) = "white" {}
		_Courners ("Rounding Courners", Range(0,0.9)) = 0.5
		_Gloss("Glossyness", 2D) = "white" {}
		_Blur ("Bluring", Range(0,1)) = 0.5
		_Stretch("Edge Courners", Vector) = (0,0,0,0)
		[Toggle] _ISUI("Is UIElement", Float) = 0
		_Offset("Offset", Range(-2,2)) = 0.5
	}

	Category{
		Tags{ 
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
			"Queue" = "Transparent"
		}

		LOD 200
		ColorMask RGBA
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#pragma multi_compile ___ _ISUI_ON
				#pragma target 3.0

				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				sampler2D _MainTex;
				float4 _MainTex_ST;
				sampler2D _Gloss;
				float4 _Gloss_ST;
				sampler2D _Map;
				float4 _Map_ST;
				float _Blur;
				float _Courners;
				float4 _Stretch;
				//float4 _ClickLight;
				float _Offset;

				struct v2f {
					float4 pos : POSITION;
					float3 viewDir : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float3 tspace0 : TEXCOORD3;
					float3 tspace1 : TEXCOORD4;
					float3 tspace2 : TEXCOORD5;
					float3 wpos : TEXCOORD6;
					float4 color : COLOR;
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.texcoord = v.texcoord.xy; //TRANSFORM_TEX(, _MainTex);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));
					o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;

					o.wpos.z += 5;
					o.wpos.xy *= 4.35;

					o.normal.xyz = normalize(UnityObjectToWorldNormal(v.normal));

					half3 wNormal = o.normal;
					half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);

					half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					half3 wBitangent = cross(wNormal, wTangent) * tangentSign;

					o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
					o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
					o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
					o.color = v.color;
					return o;
				}



				float4 frag(v2f i) : COLOR{

					#if _ISUI_ON
					_Courners = saturate((i.color.a - 0.4) * 2)*0.9;
					_Blur = saturate((1 - i.color.a));
					#endif

					float4 col = tex2Dlod(_MainTex,float4(TRANSFORM_TEX(i.texcoord.xy, _MainTex),0,_Blur * 6));

					#if _ISUI_ON
					col.a *= (1 - _Blur);
					#endif

					float2 uv = i.texcoord.xy-0.5;
					uv = abs(uv)*2;
					//_Stretch
					uv -= _Stretch;
					float2 upStretch = 1 - _Stretch;
					uv = max(0, uv) / upStretch;

					//_Courners
					uv = uv - _Courners;
					float flattened = saturate(uv.x*uv.y * 2048);
					float upscale = 1 - _Courners;
					uv = max(0, uv) / upscale;


					uv *= uv;
					float rad = (uv.x + uv.y);
					float trim = saturate((1 - rad) * 20 * (1 - _Blur) *(1 - _Courners));
					col.a *= trim;

					#if _ISUI_ON
					col.a *= saturate(i.color.a*2);
					#endif

					float2 off = i.texcoord - 0.5;
					i.viewDir.xyz = normalize(i.viewDir.xyz);
					float3 tnormal = normalize(float3( 
						(off.x)*uv.x+ off.x*0.01,
						(off.y)*uv.y+ off.y*0.01, 0.5));
					float3 worldNormal;
					worldNormal.x = dot(i.tspace0, tnormal);
					worldNormal.y = dot(i.tspace1, tnormal);
					worldNormal.z = dot(i.tspace2, tnormal);

					float courner = saturate(0.02+ _Blur+(uv.x+uv.y)*10);

					float3 lightPos = _WorldSpaceLightPos0.xyz - i.wpos.xyz;

					float dotprod = max(0, dot(worldNormal, i.viewDir.xyz));
					float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*worldNormal);
					float dott = max(0,dot(normalize(lightPos), -reflected));

					float screen = tex2D(_Gloss, TRANSFORM_TEX(i.texcoord.xy, _Gloss)).r;
		
					float power = 16 * screen + 0.0001;

					float bright = (pow(dott, 32 * (1 - _Blur)*power) //* courner
						)*power;
					col.rgb += bright*(1- _Blur)*power;

					col.a += bright * col.a;


					return col;

				}
				ENDCG
			}
		}
	}
}
