Shader "Playtime Painter/Editor/Replacement/ShadowDataOutput" {

	SubShader{

		Tags{
			"RenderType" = "Background"
			"LightMode" = "ForwardBase"
			"Queue" = "Background"
		}

		Cull Off

		Pass{

			CGPROGRAM
			#pragma vertex vertBg
			#pragma fragment fragBg
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase

			#include "Assets/Playtime-Painter/Shaders/quizcanners_built_in.cginc"

					
			float4 _SunDirection;

			struct v2fbg {
				float4 pos : SV_POSITION;
				float3 viewDir : TEXCOORD0;
			};

			v2fbg vertBg(appdata_full v) {
				v2fbg o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				return o;
			}

			float4 fragBg(v2fbg o) : COLOR{

				float3 wd = normalize(o.viewDir.xyz);

				float dott = max(0, dot(_SunDirection.xyz, -wd.xyz));


				return  float4(pow(dott, 512)*1024,0,1,1);
			}

			ENDCG
		}

	}

	SubShader{

		Tags{
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"LightMode" = "ForwardBase"
		}

		Pass{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "Assets/Playtime-Painter/Shaders/quizcanners_built_in.cginc"

				uniform sampler2D g_BakedRays_VOL;
				float4 g_BakedRays_VOLVOLUME_H_SLICES; //g_VOLUME_H_SLICES;
				float4 g_BakedRays_VOLVOLUME_POSITION_N_SIZE; //g_VOLUME_POSITION_N_SIZE;

				float4 g_BakedRays_VOL_TexelSize;

				float4 g_l0pos;
				float4 g_l0col;
				float4 g_l1pos;
				float4 g_l1col;
				float4 g_l2pos;
				float4 g_l2col;

			struct v2f {
				float4 pos : SV_POSITION;
				float4 worldPos : TEXCOORD0;
				float3 normal : TEXCOORD1;
				SHADOW_COORDS(2)
				float3 viewDir: TEXCOORD3;
				float4 shadowCoords0 : TEXCOORD4;
				float4 shadowCoords1 : TEXCOORD5;
				float4 shadowCoords2 : TEXCOORD6;
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

				float shad = GetRayTracedShadows(o.worldPos.xyz, o.normal, o.shadowCoords1,
					rt1_ProjectorConfiguration, rt1_ProjectorClipPrecompute, rt1_ProjectorPosition, float4(0,1,0,0) );


				//float4 rt_ProjectorConfiguration, float4 rt_ProjectorClipPrecompute,
				//	float4 rt_ProjectorPosition, float4 sampleMask
	
				/*float dotprod = dot(o.viewDir.xyz, o.normal);
				float fernel = (1.5 - dotprod);
				float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);
				*/

			//	float4 g_BakedRays_VOLVOLUME_H_SLICES; //g_VOLUME_H_SLICES;
			//	float4 g_BakedRays_VOLVOLUME_POSITION_N_SIZE; //g_VOLUME_POSITION_N_SIZE;

				float4 bake = SampleVolume(g_BakedRays_VOL, o.worldPos.xyz + o.normal, 
					g_BakedRays_VOLVOLUME_POSITION_N_SIZE,
					g_BakedRays_VOLVOLUME_H_SLICES);

				bake *= bake.a*bake.a;
				
				//float direct = max(0, dot(o.normal.xyz, -vec));

				//float3 halfDirection = normalize(o.viewDir.xyz - vec);
				//float NdotH = max(0.01, (dot(normal, halfDirection)));
				//float normTerm = pow(NdotH, power);

			//	return  float4(vec,1);

			//	return BounceAngle(vec, o.normal, o.viewDir.xyz, 64);

				float3 shads = 0;

				float drctnl = 0;

				shads.r = BounceAngle(drctnl, _WorldSpaceLightPos0.xyz, o.normal, o.viewDir.xyz, 64, bake.r*4);

				shads.g = BounceAngle(shad, o.worldPos.xyz - g_l1pos.xyz, o.normal, o.viewDir.xyz, 64, bake.g*0.2);

				float skyShadow = 0;
				shads.b = BounceAngle(skyShadow, o.worldPos.xyz - g_l2pos.xyz, o.normal, o.viewDir.xyz, 64, bake.b*0.5);

				//return float4(o.viewDir.xyz,1);


				

				return //float4(0,0,1,1); //
				float4(shads, 1);

			}
			ENDCG

		}

	}
}

