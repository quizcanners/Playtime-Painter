Shader "Playtime Painter/Editor/Replacement/ShadowDataOutput" 
{

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
			#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

			uniform sampler2D g_BakedShadow_VOL;
			float4 g_BakedShadows_VOL_TexelSize;

			struct v2f {
				float4 pos :		SV_POSITION;
				float3 worldPos :	TEXCOORD0;
				float3 normal :		TEXCOORD1;
				float3 viewDir:		TEXCOORD2;
				SHADOW_COORDS(3)
			};


			v2f vert(appdata_full v) {
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.normal.xyz = UnityObjectToWorldNormal(v.normal);

				o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

				TRANSFER_SHADOW(o);

				return o;
			}

			float CalculateBake(float bake, float3 pointPos, float3 lightPos, float3 viewDirection, float3 reflected, float cameraToPointLength) {

				float3 pointToLight = pointPos - lightPos;

				float3 cameraToLight = lightPos - _WorldSpaceCameraPos;

				float direct = max(0, (dot(viewDirection, normalize(cameraToLight)) - 0.99) * 100);

				direct *= saturate((cameraToPointLength - length(cameraToLight)) * 10000);

				bake = (1 - bake)*pow(max(0, dot(normalize(pointToLight), reflected)), 8)*0.5 + direct;

				return bake;

			}

			float4 frag(v2f o) : COLOR{

				o.viewDir.xyz = normalize(o.viewDir.xyz);

				o.normal = normalize(o.normal);

				float3 cameraToPoint = o.worldPos.xyz - _WorldSpaceCameraPos;

				float cameraToPointLength = length(cameraToPoint);

				float4 bake = SampleVolume(g_BakedShadow_VOL, o.worldPos,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, o.normal);

				float dotprod = dot(o.viewDir.xyz, o.normal);
				float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);


				bake.r = CalculateBake(bake.r, o.worldPos, g_l0pos.xyz, -o.viewDir, reflected, cameraToPointLength);

				bake.g = CalculateBake(bake.g, o.worldPos, g_l1pos.xyz, -o.viewDir, reflected, cameraToPointLength);

				/*float3 pointToLight = o.worldPos.xyz - g_l0pos.xyz;

				float3 cameraToLight = g_l0pos.xyz - _WorldSpaceCameraPos;

				float direct = max(0, (dot(-o.viewDir, normalize(cameraToLight)) - 0.99) * 100);

				direct *= saturate((length(cameraToPoint) - length(cameraToLight)) * 10000);

				bake.r = (1-bake.r)*pow(max(0, dot(normalize(pointToLight), reflected)), 4) + direct;

				return bake.r;*/

				/*bake.b *= max(0, dot(
					normalize(o.worldPos.xyz - _WorldSpaceLightPos0),
					reflected)
				);*/

				bake.b = max((1-bake.b), SHADOW_ATTENUATION(i))*0.5;

				return bake;

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

