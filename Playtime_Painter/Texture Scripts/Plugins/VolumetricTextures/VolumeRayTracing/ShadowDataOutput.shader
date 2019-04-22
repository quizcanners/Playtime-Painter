Shader "Playtime Painter/Editor/Replacement/ShadowDataOutput" {

	SubShader{

		Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"RayTrace" = "Opaque"
		}

		//Cull Off

		Pass{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 worldPos : TEXCOORD0;
				float3 normal : TEXCOORD1;
				SHADOW_COORDS(3)
				float3 viewDir: TEXCOORD4;
				float4 shadowCoords0 : TEXCOORD5;
				float4 shadowCoords1 : TEXCOORD6;
				float4 shadowCoords2 : TEXCOORD7;
			};

			v2f vert(appdata_full v) {
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0f));
				o.normal.xyz = UnityObjectToWorldNormal(v.normal);
				o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

				TRANSFER_SHADOW(o);

				o.shadowCoords0 = mul(rt0_ProjectorMatrix, o.worldPos);// -float4(rt0_ProjectorPosition.xyz, 0));
				o.shadowCoords1 = mul(rt1_ProjectorMatrix, o.worldPos);// -float4(rt1_ProjectorPosition.xyz, 0));
				o.shadowCoords2 = mul(rt2_ProjectorMatrix, o.worldPos);// -float4(rt2_ProjectorPosition.xyz, 0));

				return o;
			}

			inline float BounceAngle(float shadow, float3 vec, float3 normal, float3 viewDir, float power, float bake) {

				vec = normalize(vec);

				float direct = max(0, dot(normal, -vec));

				float3 halfDirection = normalize(viewDir - vec);
				float NdotH = max(0.01, (dot(normal, halfDirection)));
				float normTerm = pow(NdotH, power); 


				return  ((0.5 + normTerm* power) * shadow + bake * 0.5) * direct;
			}

			float4 frag(v2f o) : COLOR{

				o.normal = normalize(o.normal);

				o.viewDir.xyz = normalize(o.viewDir.xyz);

				float3 shads = GetRayTracedShadows(o.worldPos.xyz, o.normal, o.shadowCoords0, o.shadowCoords1, o.shadowCoords2);

	
				/*float dotprod = dot(o.viewDir.xyz, o.normal);
				float fernel = (1.5 - dotprod);
				float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);
				*/
				float4 bake = SampleVolume(g_BakedRays_VOL, o.worldPos.xyz,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, o.normal);


				float3 vec = o.worldPos.xyz - g_l0pos.xyz;

				//bake *= bake.a;
				//float direct = max(0, dot(o.normal.xyz, -vec));

				//float3 halfDirection = normalize(o.viewDir.xyz - vec);
				//float NdotH = max(0.01, (dot(normal, halfDirection)));
				//float normTerm = pow(NdotH, power);


			//	return  float4(vec,1);


			//	return BounceAngle(vec, o.normal, o.viewDir.xyz, 64);

				shads.r = BounceAngle(shads.r, vec, o.normal, o.viewDir.xyz, 64, bake.r);

				shads.g = BounceAngle(shads.g, o.worldPos.xyz - g_l1pos.xyz, o.normal, o.viewDir.xyz, 64, bake.g);

				shads.b = BounceAngle(shads.b, o.worldPos.xyz - g_l2pos.xyz, o.normal, o.viewDir.xyz, 64, bake.b);

				//return float4(o.viewDir.xyz,1);


				

				return //float4(0,0,1,1); //
				float4(shads, 1);

			}
			ENDCG

		}

	}
}

