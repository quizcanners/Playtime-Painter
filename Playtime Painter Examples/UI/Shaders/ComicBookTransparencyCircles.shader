Shader "Playtime Painter/ComicBookTransparencyCircles"
{
	Properties{
		  _MainTex("Albedo (RGB)", 2D) = "black" {}

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

					#pragma vertex vert
					#pragma fragment frag
					#pragma multi_compile_fog
					#pragma multi_compile_fwdbase
					#pragma multi_compile_instancing
					#pragma target 3.0

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

						o.screenPos = ComputeScreenPos(v.vertex);


						o.color = v.color;

						return o;
					}


					float4 frag(v2f i) : COLOR{

						i.screenPos.xy /= i.screenPos.w;

						float2 tiled = i.screenPos.xy * i.screenParams * 10;

						const float angle = 0.3;

						const float si = sin(angle);
						const float co = cos(angle);

						float tx = tiled.x;
						float ty = tiled.y;
						tiled.x = (co * tx) - (si * ty);
						tiled.y = (si * tx) + (co * ty);

						float2 flored = floor(tiled);

						tiled.y += (flored.x % 2)*0.5;

						tiled = tiled % 1;

						float rad = length(tiled - 0.5);

						float4 col = i.color * tex2Dlod(_MainTex, float4(i.texcoord.xy, 0, 0));

						col.a = max(0, col.a - rad*2) * (2 - col.a) * 10;

						col.a *= col.a;

						return col;

					}
					ENDCG
				}
			}
			Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
