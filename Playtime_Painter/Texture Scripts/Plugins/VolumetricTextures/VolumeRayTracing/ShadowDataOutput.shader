Shader "Playtime Painter/Editor/Replacement/ShadowDataOutput" {

	SubShader{

		Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"RayTrace" = "Opaque"
		}

		Pass{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

	

			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
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

				o.shadowCoords0 = mul(rt0_ProjectorMatrix, o.worldPos - float4(rt0_ProjectorPosition.xyz, 0));
				o.shadowCoords1 = mul(rt1_ProjectorMatrix, o.worldPos - float4(rt1_ProjectorPosition.xyz, 0));
				o.shadowCoords2 = mul(rt2_ProjectorMatrix, o.worldPos - float4(rt2_ProjectorPosition.xyz, 0));

				return o;
			}

			inline float BounceAngle(float3 vec, float3 normal, float3 viewDir, float power) {

				vec = normalize(vec);

				float direct = max(0, dot(normal, -vec));

				float3 halfDirection = normalize(viewDir - vec);
				float NdotH = max(0.01, (dot(normal, halfDirection)));
				float normTerm = pow(NdotH, power); 


				return  (0.5 + normTerm*2) * direct;
			}

			float4 frag(v2f o) : COLOR{

				o.normal = normalize(o.normal);

				o.viewDir.xyz = normalize(o.viewDir.xyz);

				float3 posNrm = o.worldPos.xyz + o.normal.xyz;

				float3 shads = GetRayTracedShadows(posNrm, o.shadowCoords0, o.shadowCoords1, o.shadowCoords2);

				/*float near = rt0_ProjectorConfiguration.z;

				float4 shUv0 = ProjectorUvDepthAlpha(
					o.shadowCoords0, posNrm,
					rt0_ProjectorPosition.rgb,
					rt0_ProjectorConfiguration,
					rt0_ProjectorClipPrecompute);

				shads.r = (1 - saturate((tex2D(_pp_RayProjectorDepthes, shUv0.xy).r - shUv0.z) * 128)) * shUv0.w;

				near = rt1_ProjectorConfiguration.z;

				float4 shUv1 = ProjectorUvDepthAlpha(
					o.shadowCoords1, posNrm,
					rt1_ProjectorPosition.rgb,
					rt1_ProjectorConfiguration,
					rt1_ProjectorClipPrecompute);

				shads.g = (1 - saturate((tex2D(_pp_RayProjectorDepthes, shUv1.xy).g - shUv1.z) * 128)) * shUv1.w;

				near = rt2_ProjectorConfiguration.z;

				float4 shUv2 = ProjectorUvDepthAlpha(
					o.shadowCoords2, posNrm,
					rt2_ProjectorPosition.rgb,
					rt2_ProjectorConfiguration,
					rt2_ProjectorClipPrecompute);

				shads.b = (1 - saturate((tex2D(_pp_RayProjectorDepthes, shUv2.xy).b - shUv2.z) * 128)) * shUv2.w;
				*/


				/*float dotprod = dot(o.viewDir.xyz, o.normal);
				float fernel = (1.5 - dotprod);
				float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);
				*/
				float4 bake = SampleVolume(g_BakedRays_VOL, o.worldPos,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, o.normal);


				float3 vec = o.worldPos.xyz - g_l0pos.xyz;


				//float direct = max(0, dot(o.normal.xyz, -vec));

				//float3 halfDirection = normalize(o.viewDir.xyz - vec);
				//float NdotH = max(0.01, (dot(normal, halfDirection)));
				//float normTerm = pow(NdotH, power);


			//	return  float4(vec,1);


			//	return BounceAngle(vec, o.normal, o.viewDir.xyz, 64);

				shads.r *= BounceAngle(vec, o.normal, o.viewDir.xyz, 16);

				shads.g *= BounceAngle(o.worldPos.xyz - g_l1pos.xyz, o.normal, o.viewDir.xyz, 16);

				shads.b *= BounceAngle(o.worldPos.xyz - g_l2pos.xyz, o.normal, o.viewDir.xyz, 16);

				//return float4(o.viewDir.xyz,1);


				

				return //float4(0,0,1,1); //
				float4(shads, 1);

			}
			ENDCG

		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}

	SubShader{

		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"RayTrace" = "Transparent"
		}

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

			uniform sampler2D g_BakedShadow_VOL;
			float4 g_BakedShadows_VOL_TexelSize;

			struct v2f {
				float4 pos :		SV_POSITION;
				float3 worldPos :	TEXCOORD0;
				float3 normal :		TEXCOORD1;
				float3 viewDir:		TEXCOORD2;
				float2 texcoord :	TEXCOORD3;
				SHADOW_COORDS(4)
			};


			v2f vert(appdata_full v) {
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.normal.xyz = UnityObjectToWorldNormal(v.normal);
				o.texcoord = v.texcoord.xy;
				o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

				TRANSFER_SHADOW(o);

				return o;
			}

			float4 frag(v2f o) : COLOR{

				float2 off = o.texcoord - 0.5;
				off *= off;

				o.viewDir.xyz = normalize(o.viewDir.xyz);

				o.normal = normalize(o.normal);


				float3 cameraToPoint = o.worldPos.xyz - _WorldSpaceCameraPos;

				float cameraToPointDistance = length(cameraToPoint);

				float distance = (10 - max(0, 10 - cameraToPointDistance))*0.1;

				float alpha = max(0, (1 - (off.x + off.y) * 4)*abs(dot(o.viewDir.xyz, o.normal.xyz)))*distance;


				float dotprod = dot(o.viewDir.xyz, o.normal);
				float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);

				// Point Lights:
				float4 bake = SampleVolume(g_BakedShadow_VOL, o.worldPos,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, o.normal);

				float3 cameraToLight = g_l0pos.xyz - _WorldSpaceCameraPos;


				float direct = max(0, (dot(-o.viewDir, normalize(cameraToLight)) - 0.99) * 100);

				float3 pointToLight = o.worldPos.xyz - g_l0pos.xyz;

				direct *= saturate((cameraToPointDistance - length(cameraToLight)) * 10000);

				bake.r = (1 - bake.r)*pow(max(0,
					dot(normalize(pointToLight),
					reflected)), 4) + direct;

				return bake.r * alpha;

				bake.g *= max(0, dot(
					normalize(o.worldPos.xyz - g_l1pos.xyz),
					reflected));

				bake.b *= max(0, dot(
					normalize(o.worldPos.xyz - _WorldSpaceLightPos0),
					reflected)
				);

				bake.b = max(bake.b, SHADOW_ATTENUATION(i));

			}
			ENDCG

		}
	}


}

