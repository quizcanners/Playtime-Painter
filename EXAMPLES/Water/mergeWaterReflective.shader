Shader "PlaytimePainter/Water" {
	Properties {
		_BumpMapC("BumpMap (RGB)", 2D) = "grey" {}
	}

	Category {
			Tags { "RenderType"="Transparent" 
			 "LightMode"="ForwardBase"
			   "Queue" = "Geometry+50"
			 }
				LOD 200
				 	Blend OneMinusDstAlpha DstAlpha // This makes water invisible without using transparency
			ColorMask RGBA


	SubShader {
		Pass {
			

	
		CGPROGRAM
	
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

		sampler2D _BumpMapC;

			struct v2f {
				float4 pos : POSITION;
				float4 viewDir : TEXCOORD1;
				float3 wpos : TEXCOORD2;
				UNITY_FOG_COORDS(3)
				SHADOW_COORDS(4) 
				float3 tc_Control : TEXCOORD5;
			};

			v2f vert (appdata_full v) {
				v2f o;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.wpos = mul (unity_ObjectToWorld, v.vertex).xyz;
				o.viewDir.xyz=WorldSpaceViewDir(v.vertex);

			
				o.tc_Control.xyz = (o.wpos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;

				o.viewDir.w = 0;
				UNITY_TRANSFER_FOG(o, o.pos);
				TRANSFER_SHADOW(o);

				 float3 worldNormal = UnityObjectToWorldNormal(v.normal);

			//	 o.diff = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz)) * _LightColor0;


				// o.normal = normalize(worldNormal);// * _LightColor0;
				


				return o;
			}


			float4 frag (v2f i) : COLOR {
				float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz);

			float far = min(1, dist*0.01);
			float deFar = 1 - far;

			i.viewDir.xyz = normalize(i.viewDir.xyz);
			
			
			float2 waterUV = (i.wpos.xz- _mergeTeraPosition.xz)*0.02;
			float2 waterUV2 = waterUV.yx;//*0.01;


			float4 bump2B = tex2D(_BumpMapC, waterUV * 0.1 + _Time.y*0.0041);
			bump2B.rg -= 0.5;
			
			float4 bumpB = tex2D(_BumpMapC, waterUV2 * 0.1 - _Time.y*0.005);
			bumpB.rg -= 0.5;

			float4 bump2 = tex2Dlod(_BumpMapC, float4(waterUV + bumpB.rg*0.01
				- _Time.y*0.02,0, bump2B.a*bumpB.a*2));
			bump2.rg -= 0.5;

			float4 bump = tex2Dlod(_BumpMapC,float4(waterUV2 - bump2.rg*0.02
				+ bump2.rg*0.01 + _Time.y*0.032 , 0 , bumpB.a *bump2B.a*2));
			bump.rg -= 0.5;
		
		


			bump.rg = (bump2.rg //*bump.a
				+ bump.rg)*deFar + (bump2B.rg*bump.a //*bump.a
					+ bumpB.rg*bump2.a)*0.5;//*bump2.a;// )*0.1;
			

			//bump.b *= bump2.b;

			float smoothness = saturate(bump.b+bump2.b*deFar);
			float deSmoothness = 1 - smoothness;
			//smoothness *= smoothness;

			float3 normal = normalize(float3(bump.r,1,bump.g));

			float3 preDot = normal*i.viewDir.xyz;

			float dotprod = (preDot.x + preDot.y + preDot.z);// / 1024;//
				//dot(normal,  i.viewDir.xyz);
			float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*normal);

			float shadow = SHADOW_ATTENUATION(i);

			float4 cont = 0;
			
			float dott = max(0.1,dot(_WorldSpaceLightPos0, -reflected));

			dotprod = (1 - dotprod);//*0.75;
			//float dottToView = dot(i.viewDir.xyz, )

			float4 terrainSampling = float4(i.tc_Control.xz - reflected.xz*smoothness*0.05, 0, 5 * deSmoothness);

			float4 terrainLight = tex2Dlod(_TerrainColors, terrainSampling);
			//terrainLight.rgb *=_LightColor0.rgb;

			float height = max(0, tex2D(_mergeTerrainHeight, terrainSampling).a);


			float3 halfDirection = normalize(i.viewDir.xyz + _WorldSpaceLightPos0.xyz);

			float NdotH = saturate((dot(normal, halfDirection)-1+smoothness*0.005)*100);


			cont.rgb = ( unity_AmbientSky.rgb*max(0,terrainLight.a*terrainLight.a- height* height*2) + (dott*0.1
				+ NdotH*4)*shadow*_LightColor0.rgb
				+terrainLight.rgb *bump.b*0.5) *(0.25+ dotprod);

			BleedAndBrightness(cont, 1);

			UNITY_APPLY_FOG(i.fogCoord, cont);

			return cont;

			}


		ENDCG
	}
	  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
}
}
}
