Shader "MilkLab/EtiPuf/ImageTransition" {
	Properties{
		[PerRendererData]_MainTex("Mask (RGB)", 2D) = "white" {}
		_MainTex_Current("First Texture", 2D) = "black" {}
		_Next_MainTex("Next Texture", 2D) = "black" {}
		_Transition("Transition", Range(0,1)) = 0
		_Overlay("Overlay", 2D) = "black" {}
		_Mask("Mask", 2D) = "white" {}
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

				#include "Assets/Tools/quizcanners/VertexDataProcessInclude.cginc"

				sampler2D _MainTex_Current;
				float4 _MainTex_Current_TexelSize;
				sampler2D _Next_MainTex;
				float4 _MainTex_Current_ST;
				sampler2D _Overlay;
				sampler2D _Mask;

				float _Transition;

				sampler2D _Map;
				float4 _Map_ST;


				struct v2f {
					float4 pos : POSITION;
					float3 viewDir : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float4 texcoord : TEXCOORD2;
					float3 tspace0 : TEXCOORD3;
					float3 tspace1 : TEXCOORD4;
					float3 tspace2 : TEXCOORD5;
					float4 screenPos : TEXCOORD7;
					float4 color : COLOR;
				};


				v2f vert(appdata_full v) {
					v2f o;

					o.texcoord.xy = v.texcoord.xy;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.screenPos = ComputeScreenPos(o.pos);
					o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));

					float2 scale = _MainTex_Current_TexelSize.zw;
					o.texcoord.zw = float2 (max(0, (scale.x - scale.y) / scale.x), max(0, (scale.y - scale.x) / scale.y));

					o.normal.xyz = normalize(UnityObjectToWorldNormal(v.normal));

					half3 wNormal = o.normal;
					half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);

					half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					half3 wBitangent = cross(wNormal, wTangent) * tangentSign;

					o.color = v.color;
					return o;
				}


				float4 frag(v2f i) : COLOR{

					float2 texUV = i.texcoord.xy;//, _MainTex);

					texUV -= 0.5;

					float len = length(texUV);

					texUV *= 1 + (saturate(1 - i.color.a));

					texUV += 0.5;

					float _Courners = saturate((i.color.a - 0.4) * 2)*0.9;
					float _Blur = saturate((1 - i.color.a));
					float4 overlay = tex2D(_Overlay, texUV);
					//_Mask
					float4 mask = tex2D(_Mask, texUV);

					float2 screenUV = i.screenPos.xy / i.screenPos.w;


					float4 col = tex2Dlod(_MainTex_Current,float4(texUV, 0, 0))*(1 - _Transition)
						+ tex2Dlod(_Next_MainTex, float4(texUV, 0, 0))*(_Transition);

					col.a *= mask.a;

					col = col * (1 - overlay.a) + overlay * overlay.a;

					col.a *= (1 - _Blur);

					return saturate(col);
				}
				ENDCG
			}
		}
	}
}
