Shader "Playtime Painter/ComicBookTransparencyCircles"
{
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "black" {}
		[KeywordEnum(circle, hexagon)] _shape("Shape ", Float) = 0
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
		ZTest Off
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

				struct v2f {
					float4 pos : SV_POSITION;
					float2 screenParams : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float4 screenPos : 	TEXCOORD3;
					float4 color: COLOR;
				};

				sampler2D _MainTex;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					float aspect = _ScreenParams.x / _ScreenParams.y;

					o.screenParams = float2(max(_ScreenParams.x / _ScreenParams.y, 1), max(1, _ScreenParams.y / _ScreenParams.x));

					o.screenPos = ComputeScreenPos(o.pos);

					o.color = v.color;

					return o;
				}

				float4 frag(v2f i) : COLOR{

					i.screenPos.xy /= i.screenPos.w;

					float2 fitToScreen = i.screenPos.xy * i.screenParams;

					const float angle = 0.3;

					const float si = sin(angle);
					const float co = cos(angle);

					float tx = fitToScreen.x;
					float ty = fitToScreen.y;
					fitToScreen.x = (co * tx) - (si * ty);
					fitToScreen.y = (si * tx) + (co * ty);

					float4 col = i.color;

					float sharpness = (1.5 - i.color.a) * 10;

					float alpha = i.color.a;

					const float tiling = 4;



					float2 grid = fitToScreen * tiling;

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
						
					/*float2 r = float2(1, 1.73);

					float2 h = r * 0.5;

					float2 gridB = grid + h;

					float2 floorA = floor(grid / r);

					float2 floorB = floor(gridB / r);

					float2 uvA = ((grid - floorA * r) - h) * 2;

					float2 uvB = ((gridB - floorB * r) - h) * 2;

					float distA = GetHexagon(uvA);

					float distB = GetHexagon(uvB);

					float isB = saturate((distA - distB)*9999);

					float dist =  distB * isB + distA * (1 - isB);

					float2 index = floorA * (1 - isB) + (floorB-0.5) * isB;

					col.rg = index * 0.1;

					col.b = 0;*/

					float4 hex = GetHexagons(grid);

					float dist = hex.z;

					col.rgb = hex.w;//float3(hex.xy * 0.1, hex.w);

					col.rgb = tex2D(_MainTex, hex.zw).rgb;

					col.a =( 1 - (dist - (sin(hex.x*hex.y + _Time.w) + 1)*0.5) * sharpness) * alpha;

#endif

					return col;

				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
}
}
