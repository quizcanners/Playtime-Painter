Shader "Playtime Painter/Geometry/Ray Tracing/VertexColor" {
	Properties {

	}

	Category{
		Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"RayTrace" = "Opaque"
			"Volume" = "g_BakedRays_VOL"
		}

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

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
					float4 shadowCoords0 : TEXCOORD7;
					float4 shadowCoords1 : TEXCOORD8;
					float4 shadowCoords2 : TEXCOORD9;
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					UNITY_TRANSFER_FOG(o, o.pos);
					o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0f));
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
			
					o.vcol = v.color;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					o.texcoord = v.texcoord.xy;
					o.edge = v.texcoord3;

					TRANSFER_SHADOW(o);

					// TODO: Need for subtracting a position is an unexpected behavious

					o.shadowCoords0 = mul(rt0_ProjectorMatrix, o.worldPos - float4(rt0_ProjectorPosition.xyz, 0));
					o.shadowCoords1 = mul(rt1_ProjectorMatrix, o.worldPos - float4(rt1_ProjectorPosition.xyz, 0));
					o.shadowCoords2 = mul(rt2_ProjectorMatrix, o.worldPos - float4(rt2_ProjectorPosition.xyz, 0));

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

					scatter += bake * lcol.rgb;

					lcol.rgb *= direct;

					glossLight += lcol.rgb*normTerm;
					directLight += lcol.rgb / (len * len) * shadow;
				}

			

				float4 frag(v2f o) : COLOR{

					o.normal = normalize(o.normal);

					o.viewDir.xyz = normalize(o.viewDir.xyz);

					float3 shads = GetRayTracedShadows(o.worldPos.xyz, o.normal, o.shadowCoords0, o.shadowCoords1, o.shadowCoords2);

					float4 col = 0.5; //o.vcol;

					float4 bumpMap = float4(0,0,0.5,1);
	
					float dotprod = dot(o.viewDir.xyz, o.normal);
					float fernel = (1.5 - dotprod);
					//float ambientBlock = (3+dotprod);
					float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);
			
					// Point Lights:
			
					float4 bake = SampleVolume(g_BakedRays_VOL, o.worldPos,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, o.normal);

					float power = bumpMap.b; 

					float3 scatter = 0;
					float3 glossLight = 0;
					float3 directLight = 0;

					//return shads.r;

					PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l0pos.xyz,
						o.normal, o.viewDir.xyz, bake.r, shads.r,  g_l0col, power);

					PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l1pos.xyz,
						o.normal, o.viewDir.xyz, bake.g, shads.g,  g_l1col, power);

					PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l2pos.xyz,
						o.normal, o.viewDir.xyz, bake.b, shads.b,  g_l2col, power);

					col.rgb *= 
						directLight
						+ 
						scatter * 0.0005
						;

					float smoothness = saturate(pow(col.a, 5 - fernel));
					float deDmoothness = 1 - smoothness;
			
					return col;//  +bake * 0.25;

				}
				ENDCG

			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
		FallBack "Diffuse"
	}
}

