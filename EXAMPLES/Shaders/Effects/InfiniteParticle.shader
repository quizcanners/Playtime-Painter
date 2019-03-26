Shader "Playtime Painter/Effects/InfiniteParticle" {
	Properties{
		[NoScaleOffset]_MainTex("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset]_MainTex2("Albedo (RGB)", 2D) = "white" {}
		_Speed("Speed", Range(0,60)) = 2
		_CustomTime("Time", Range(0,60)) = 2
		_Tiling("Tiling", Range(0.1,20)) = 2
		_Upscale("Forward", Range(0.1,1)) = 1
			[Toggle(_FADEOUT)] unlinked("Fade Out On Alpha One", Float) = 0
	}

	Category{

		ColorMask RGB
		Cull Off
		ZWrite Off
		Blend SrcAlpha One //MinusSrcAlpha

		SubShader{

			Tags{
				"Queue" = "Overlay"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
			}

			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma shader_feature ____  _FADEOUT 
				#pragma shader_feature SCREENSPACE
				#pragma shader_feature DYNAMIC_SPEED
				#pragma target 3.0

				struct v2f {
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float4 col01UVs : TEXCOORD1;
					float4 col23UVs : TEXCOORD2;
					float4 colPortions : TEXCOORD3;
					float3 offAndTotal : TEXCOORD4;
					#if SCREENSPACE
					float4 screenPos : TEXCOORD5;
					#endif
					float4 color: COLOR;
				};

				float _Speed;
				float _CustomTime;
				float _Tiling;
				float _Upscale;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					#if SCREENSPACE
					o.screenPos = ComputeScreenPos(o.pos);
					#endif
					o.color = v.color;
					o.color.rgb = normalize(o.color.rgb) * 3;

					const float PI = 3.14159265359;
#if DYNAMIC_SPEED
					float t = _CustomTime;
#else
					float t = _Time.x*_Speed;
#endif
					float2 off = (o.texcoord - 0.5);
					o.offAndTotal.xy = off;
					
					off*=_Upscale;
					float2 rand = float2(_Time.x*0.05, _Time.x *0.05* 2.3);

					float alpha = tan(t) + 1;
					o.colPortions.x = max(0, 1 - abs(alpha - 1));
					o.col01UVs.xy = rand * 0.7 + off * (_Tiling * (2 - alpha));

					alpha = tan(t + PI * 0.5) + 1;
					o.colPortions.y = max(0, 1 - abs(alpha - 1));
					o.col01UVs.zw = rand * 1.1 + off * (_Tiling * (2 - alpha));

					alpha = tan(t + PI * 0.25) + 1;
					o.colPortions.z = max(0, 1 - abs(alpha - 1));
					o.col23UVs.xy =  rand * 1.3 + off * (_Tiling * (2 - alpha));

					alpha = tan(t + PI * 0.75) + 1;
					o.colPortions.w = max(0, 1 - abs(alpha - 1));
					o.col23UVs.zw =  rand * 1.7 + off * (_Tiling * (2 - alpha));

				

					o.colPortions *= o.colPortions;

					o.offAndTotal.z = o.colPortions.x + o.colPortions.y + o.colPortions.z + o.colPortions.w + 0.00001;

				

					return o;
				}

				sampler2D _MainTex;
				sampler2D _MainTex2;
				float4 _MainTex_TexelSize;

				float4 frag(v2f i) : COLOR{

					float2 off = i.offAndTotal.xy;

					float distance = dot(off, off)*4;

#if _FADEOUT
					float finalAlpha = saturate(i.color.a * 2 - distance);

					

					finalAlpha *= saturate((1 - i.color.a) * (2 + distance));




#else
					float finalAlpha = saturate(1 - distance);
#endif
					

#if SCREENSPACE
					float2 sp = (i.screenPos.xy / i.screenPos.w) * _ScreenParams.xy * _MainTex_TexelSize.xy;
#else
					float2 sp = i.texcoord.xy;
#endif

					float4 col = tex2Dlod(_MainTex, float4(sp + i.col01UVs.xy,0,0));
					
					float4 col2 = tex2Dlod(_MainTex2, float4(sp + i.col01UVs.zw, 0, 0));

					float4 col3 = tex2Dlod(_MainTex, float4(sp + i.col23UVs.xy, 0, 0));

					float4 col4 = tex2Dlod(_MainTex2, float4(sp + +i.col23UVs.zw, 0, 0));

					col = (col * i.colPortions.x + col2 * i.colPortions.y + col3 * i.colPortions.z + col4 * i.colPortions.w) / i.offAndTotal.z * pow(finalAlpha , 4);

					//_FADEOUT
					col.a *= finalAlpha; 

					col.rgb *= i.color.rgb;

					float3 mix = col.gbr + col.brg;
					col.rgb += mix * mix*0.05;

					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}

	CustomEditor "InfiniteParticlesDrawerGUI"
}