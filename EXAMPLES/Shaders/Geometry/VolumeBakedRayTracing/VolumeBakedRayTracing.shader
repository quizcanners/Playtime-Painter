Shader "Playtime Painter/Geometry/Baked Ray Tracing/VertexColor" {
	Properties {

	}

	Category{
		Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"RayTrace" = "Opaque"
			"Volume" = "Global"
		}

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				uniform sampler2D g_BakedShadow_VOL;
				float4 g_BakedShadows_VOL_TexelSize;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 vcol : COLOR0;

					float3 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					SHADOW_COORDS(3)
					float3 viewDir: TEXCOORD4;
					float4 edge : TEXCOORD5;

					UNITY_FOG_COORDS(6)
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
					o.edge = v.texcoord3;

					TRANSFER_SHADOW(o);

					return o;
				}


				inline void PointLightTrace(inout float3 scatter, inout float3 glossLight, inout float3 directLight,
					float3 vec, float3 normal, float3 viewDir, float bake, float shadow, float4 lcol, float power) {

					float len = length(vec);
					vec /= len;

					float direct = max(0, dot(normal, -vec));

					float3 halfDirection = normalize(viewDir - vec);
					float NdotH = max(0.01, (dot(normal, halfDirection)));
					float normTerm = pow(NdotH, power); // GGXTerm(NdotH, power);

					scatter += bake*lcol.rgb;

					lcol.rgb *= direct;

					glossLight += lcol.rgb*normTerm;
					directLight += lcol.rgb / (len * len);
				}


				float4 frag(v2f i) : COLOR{

					float2 border = DetectEdge(i.edge);
					border.x = max(border.y, border.x);
					float deBorder = 1 - border.x;
					
					float4 col = i.vcol;

					float4 bumpMap = float4(0,0,0.5,1);
	
					bumpMap.ba = bumpMap.ba*deBorder + float2(1, 1)*border.x;
		
					i.viewDir.xyz = normalize(i.viewDir.xyz);

					i.normal = normalize(i.normal);

					float dotprod = dot(i.viewDir.xyz, i.normal);
					float fernel = (1.5 - dotprod);
					float ambientBlock = (3+dotprod);
					float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal);
			
					// Point Lights:
			
					float4 bake = SampleVolume(g_BakedShadow_VOL, i.worldPos,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, i.normal);

					bake = 1 - bake;

					float power = bumpMap.b; 

					float3 scatter = 0;
					float3 glossLight = 0;
					float3 directLight = 0;

					PointLightTrace(scatter, glossLight, directLight, i.worldPos.xyz - g_l0pos.xyz,
						i.normal, i.viewDir.xyz, ambientBlock, bake.r,  g_l0col, power);

					PointLightTrace(scatter, glossLight, directLight, i.worldPos.xyz - g_l1pos.xyz,
						i.normal, i.viewDir.xyz, ambientBlock, bake.g,  g_l1col, power);

					PointLightTrace(scatter, glossLight, directLight, i.worldPos.xyz - g_l2pos.xyz,
						i.normal, i.viewDir.xyz, ambientBlock, bake.b,  g_l2col, power);

					glossLight *= 0.1;
					scatter *= (1 - bake.a);

					float shadow = SHADOW_ATTENUATION(i);

					DirectionalLight(scatter, glossLight, directLight,
						shadow, i.normal.xyz, i.viewDir.xyz, ambientBlock, bake.a, power);

					float smoothness = saturate(pow(col.a, 5 - fernel));
					float deDmoothness = 1 - smoothness;

					col.rgb *= (directLight*deDmoothness + (scatter)* bumpMap.a	);

					col.rgb += (glossLight + ShadeSH9(float4(-reflected, 1)))* smoothness;

					BleedAndBrightness(col, 1+shadow*8);

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

