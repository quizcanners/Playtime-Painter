Shader "Playtime Painter/Volumes/HighResolutionVolume" {
	Properties{

			[NoScaleOffset]_Volume("Baked Shadow Volume (RGB)", 2D) = "grey" {}
			VOLUME_H_SLICES("Baked Shadow Slices", Vector) = (0,0,0,0)
			VOLUME_POSITION_N_SIZE("Baked Shadow Position & Size", Vector) = (0,0,0,0)
	}

	Category{
		Tags{ "Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"VertexColorRole_A" = "Second Atlas Texture"
			"VertexColorRole_B" = "Additional Wetness"
		}

		SubShader{
			Pass{

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma target 3.0
				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				uniform sampler2D _Volume;
				float4 _Volume_ST;
				float4 _Volume_TexelSize;

				float4 VOLUME_H_SLICES;
				float4 VOLUME_POSITION_N_SIZE;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 vcol : COLOR0;

					float3 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float2 texcoord2 : TEXCOORD3;
					SHADOW_COORDS(4)
					float3 viewDir: TEXCOORD5;
					float4 edge : TEXCOORD6;

					UNITY_FOG_COORDS(7)
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					UNITY_TRANSFER_FOG(o, o.pos);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);

					o.vcol = v.color;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					o.texcoord = v.texcoord.xy;
					o.texcoord2 = v.texcoord1.xy;
					o.edge = v.texcoord3;

					TRANSFER_SHADOW(o);

					return o;
				}

				float4 frag(v2f i) : COLOR{

					float2 border = DetectEdge(i.edge);
					border.x = max(border.y, border.x);// border.y);
					float deBorder = 1 - border.x;


						i.viewDir.xyz = normalize(i.viewDir.xyz);

						i.normal = normalize(i.normal);

						float dotprod = dot(i.viewDir.xyz, i.normal);
						float fernel = (1.5 - dotprod);

						float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal);

						float4 col = SampleVolume(_Volume, i.worldPos,  VOLUME_POSITION_N_SIZE,  VOLUME_H_SLICES, i.normal);

						col = col * deBorder + i.vcol*border.x;

						float shadow = SHADOW_ATTENUATION(i);

						float smoothness = saturate(pow(col.a, 5 - fernel));
						float deDmoothness = 1 - smoothness;

						BleedAndBrightness(col, 1 + shadow * 8);

						UNITY_APPLY_FOG(i.fogCoord, col);

						return col;

				}
							ENDCG

			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
		FallBack "Diffuse"
	}

}

