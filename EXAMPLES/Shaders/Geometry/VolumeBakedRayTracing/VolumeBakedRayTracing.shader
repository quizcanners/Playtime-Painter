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
			"Volume" = "Global"
		}

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				uniform sampler2D g_BakedRays_VOL;
				float4 g_BakedRays_VOL_TexelSize;

				float4x4 rt0_ProjectorMatrix;
				float4 rt0_ProjectorPosition;
				float4 rt0_ProjectorClipPrecompute;
				float4 rt0_ProjectorConfiguration;

				float4x4 rt1_ProjectorMatrix;
				float4 rt1_ProjectorPosition;
				float4 rt1_ProjectorClipPrecompute;
				float4 rt1_ProjectorConfiguration;

				float4x4 rt2_ProjectorMatrix;
				float4 rt2_ProjectorPosition;
				float4 rt2_ProjectorClipPrecompute;
				float4 rt2_ProjectorConfiguration;

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
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
			
					o.vcol = v.color;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					o.texcoord = v.texcoord.xy;
					o.edge = v.texcoord3;

					TRANSFER_SHADOW(o);

					o.shadowCoords0 = mul(rt0_ProjectorMatrix, o.worldPos);
					o.shadowCoords1 = mul(rt1_ProjectorMatrix, o.worldPos);
					o.shadowCoords2 = mul(rt2_ProjectorMatrix, o.worldPos);

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

				float3 ProjectorUV(float4 shadowCoords, float3 worldPos, float3 lightPos, float camAspectRatio, float camFOVDegrees, float deFar,
					float4 precompute) {

					shadowCoords.xy /= shadowCoords.w;

					float alpha = max(0, sign(shadowCoords.w) - dot(shadowCoords.xy, shadowCoords.xy));

					float viewPos = length(float3(shadowCoords.xy * camFOVDegrees, 1))*camAspectRatio;

					float true01Range = length(worldPos - lightPos) * deFar;

					float predictedDepth = 1 - (((viewPos / true01Range) - precompute.y) * precompute.z);

					float3 uv = float3((shadowCoords.xy + 1) * 0.5, predictedDepth);

					return uv; //tex2D(depthTex, uv) - predictedDepth;
				}

				float4 frag(v2f o) : COLOR{

					float3 shads;

					float3 shUv0 = ProjectorUV( 
						o.shadowCoords0, o.worldPos, 
						rt0_ProjectorPosition.rgb,
						rt0_ProjectorConfiguration.x, 
						rt0_ProjectorConfiguration.y, 
						rt0_ProjectorConfiguration.w,
						rt0_ProjectorClipPrecompute);

					shads.r = tex2D(g_BakedRays_VOL, shUv0.xy).r - shUv0.z;

					float3 shUv1 = ProjectorUV(
						o.shadowCoords1, o.worldPos,
						rt1_ProjectorPosition.rgb,
						rt1_ProjectorConfiguration.x,
						rt1_ProjectorConfiguration.y,
						rt1_ProjectorConfiguration.w,
						rt1_ProjectorClipPrecompute);

					shads.g = tex2D(g_BakedRays_VOL, shUv1.xy).g - shUv1.z;

					float3 shUv2 = ProjectorUV(
						o.shadowCoords2, o.worldPos,
						rt2_ProjectorPosition.rgb,
						rt2_ProjectorConfiguration.x,
						rt2_ProjectorConfiguration.y,
						rt2_ProjectorConfiguration.w,
						rt2_ProjectorClipPrecompute);

					shads.b = tex2D(g_BakedRays_VOL, shUv2.xy).b - shUv2.z;

					return float4 (shads, 1 );
				
					float2 border = DetectEdge(o.edge);
					border.x = max(border.y, border.x);
					float deBorder = 1 - border.x;
					
					float4 col = o.vcol;

					float4 bumpMap = float4(0,0,0.5,1);
	
					bumpMap.ba = bumpMap.ba*deBorder + float2(1, 1)*border.x;
		
					o.viewDir.xyz = normalize(o.viewDir.xyz);

					o.normal = normalize(o.normal);

					float dotprod = dot(o.viewDir.xyz, o.normal);
					float fernel = (1.5 - dotprod);
					float ambientBlock = (3+dotprod);
					float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);
			
					// Point Lights:
			
					float4 bake = SampleVolume(g_BakedRays_VOL, o.worldPos,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, o.normal);

					bake = 1 - bake;

					float power = bumpMap.b; 

					float3 scatter = 0;
					float3 glossLight = 0;
					float3 directLight = 0;

					PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l0pos.xyz,
						o.normal, o.viewDir.xyz, ambientBlock, bake.r,  g_l0col, power);

					PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l1pos.xyz,
						o.normal, o.viewDir.xyz, ambientBlock, bake.g,  g_l1col, power);

					PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l2pos.xyz,
						o.normal, o.viewDir.xyz, ambientBlock, bake.b,  g_l2col, power);

					glossLight *= 0.1;
					scatter *= (1 - bake.a);

					float shadow = SHADOW_ATTENUATION(o);

					DirectionalLight(scatter, glossLight, directLight,
						shadow, o.normal.xyz, o.viewDir.xyz, ambientBlock, bake.a, power);

					float smoothness = saturate(pow(col.a, 5 - fernel));
					float deDmoothness = 1 - smoothness;

					col.rgb *= (directLight*deDmoothness + (scatter)* bumpMap.a	);

					col.rgb += (glossLight + ShadeSH9(float4(-reflected, 1)))* smoothness;

					BleedAndBrightness(col, 1+shadow*8);

					UNITY_APPLY_FOG(o.fogCoord, col);

					return col;

				}
				ENDCG

			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
		FallBack "Diffuse"
	}
}

