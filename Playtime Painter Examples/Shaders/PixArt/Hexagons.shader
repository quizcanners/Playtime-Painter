Shader "Playtime Painter/Pixel Art/Hexagons"
{
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "black" {}
		[KeywordEnum(circle, hexagon)] _shape("Shape ", Float) = 0
		[KeywordEnum(screen, tex)] _space("Space ", Float) = 0
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"PreviewType" = "Plane"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		//ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"
				#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_cg.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing

				#pragma shader_feature _SHAPE_CIRCLE _SHAPE_HEXAGON
				#pragma shader_feature _SPACE_SCREEN  _SPACE_TEX

				struct v2f {
					float4 pos : SV_POSITION;
					float2 screenParams : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float4 screenPos : 	TEXCOORD3;
					float4 color: COLOR;
				};

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;

					o.screenParams = float2(max(_ScreenParams.x / _ScreenParams.y, 1), max(1, _ScreenParams.y / _ScreenParams.x));

					o.screenPos = ComputeScreenPos(o.pos);

					o.color = v.color;

					return o;
				}

				float GetHexagon(float2 uv) {
					uv = abs(uv);

					const float2 toDot = normalize(float2(1, 1.73));

					float c = dot(uv, toDot);

					return max(c, uv.x);
				}


				inline float4 GetHexagons(float2 grid, float2 texelSize) 
				{

					grid = grid * 1.03 - float2(0.03, 0.06)*texelSize;

					const float2 r = float2(1, 1.73);

					const float2 h = r * 0.5;

					float2 gridB = grid + h;

					float2 floorA = floor(grid / r);

					float2 floorB = floor(gridB / r);

					float2 uvA = ((grid - floorA * r) - h);

					float2 uvB = ((gridB - floorB * r) - h);

					float distA = GetHexagon(uvA);

					float distB = GetHexagon(uvB);

					float isB = saturate((distA - distB) * 9999);

					float dist = (distB * isB + distA * (1 - isB))*2;

					const float2 deChecker = float2(1, 2);

					float2 index = floorA * deChecker * (1 - isB) + (floorB * deChecker - deChecker + 1) * isB;

					//float2 uv = uvA * (1 - isB) + uvB * isB;

					const float pii = 3.141592653589793238462;

					const float pi2 = 1.0 / 6.283185307179586476924;

					float angle = 0;//(atan2(uv.x, uv.y) + pii) * pi2;

					return float4(index, dist, angle);

				}


				float4 frag(v2f i) : COLOR{


					

					float2 grid;

					#if _SPACE_SCREEN
						i.screenPos.xy /= i.screenPos.w;

						float2 fitToScreen = i.screenPos.xy * i.screenParams;

						const float angle = 0.3;

						const float si = sin(angle);
						const float co = cos(angle);

						float tx = fitToScreen.x;
						float ty = fitToScreen.y;
						fitToScreen.x = (co * tx) - (si * ty);
						fitToScreen.y = (si * tx) + (co * ty);

						grid = fitToScreen * 8;

					#elif _SPACE_TEX
						grid = i.texcoord.xy *  _MainTex_TexelSize.zw;
					#endif


					float4 col = i.color;

					float sharpness = (1.5 - i.color.a) * 10;

					float alpha = i.color.a;


#if _SHAPE_CIRCLE 
					
					col *= tex2Dlod(_MainTex, float4(i.texcoord.xy, 0, 0));

					float2 flored = floor(grid);

					float offset = (flored.x % 2);

					grid.y += offset * 0.5;

					flored = floor(grid);

					grid = grid % 1;

					float2 uv = (grid - 0.5) * 2; 

					float rad = dot(uv, uv);

					col.rg = (flored) * 0.1;

					col.a = max(0, i.color.a - rad) * sharpness;

					col.a *= col.a;

#elif _SHAPE_HEXAGON
						
					float4 hex = GetHexagons(grid, _MainTex_TexelSize.zw);

					float dist = hex.z;



					#if _SPACE_SCREEN
					float2 uv = hex.zw;
						col.rgb = tex2D(_MainTex, uv).rgb;
					#elif _SPACE_TEX
					
					float2 uv = (hex.xy+0.5) * _MainTex_TexelSize.xy;
		
						col *= tex2D(_MainTex, uv);

						float2 cut = max(0, max(-uv, uv - 1) * 9999);

						alpha *= saturate(1 - (cut.x+cut.y));


	
					#endif

						col.a = saturate((col.a - dist) * (1.5 - col.a) * 10) * alpha;
					

#endif

					col.rgb *= col.a;

					//col.a = 1;
					

					return col;

				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
}
}
