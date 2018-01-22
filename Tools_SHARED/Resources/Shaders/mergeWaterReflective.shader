Shader "Water/Reflective" {
	Properties {
		//_Color ("Color", Color) = (1,1,1,1)
		_BumpMapC("BumpMap (RGB)", 2D) = "white" {}
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
		#include "UnityLightingCommon.cginc" 
			 #include "Lighting.cginc"
			#include "UnityCG.cginc"
			 #include "AutoLight.cginc"

#include "VertexDataProcessInclude.cginc"

		#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED


		//sampler2D _mergeControl;
		sampler2D _BumpMapC;


			struct v2f {
				float4 pos : POSITION;
				//float4 diff : COLOR;
				float4 viewDir : TEXCOORD1; // 
				float3 wpos : TEXCOORD2;
				UNITY_FOG_COORDS(3)
				SHADOW_COORDS(4) 
					float3 tc_Control : TEXCOORD5;
			//	float3 normal : TEXCOORD5;
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

			float4 bump2 = tex2Dlod(_BumpMapC, float4(waterUV + bumpB.rg*0.02
				- _Time.y*0.02,0, bump2B.a*bumpB.a*2));
			bump2.rg -= 0.5;

			float4 bump = tex2Dlod(_BumpMapC,float4(waterUV2 - bump2.rg*0.02
				+ bump2.rg*0.05 + _Time.y*0.032 , 0 , bumpB.a *bump2B.a * 2));
			bump.rg -= 0.5;
		
		


			bump.rg = (bump2.rg //*bump.a
				+ bump.rg)*deFar; + (bump2B.rg*bump.a //*bump.a
					+ bumpB.rg*bump2.a)*0.5;//*bump2.a;// )*0.1;
			

			//bump.b *= bump2.b;

			float smoothness = bump.b+bump2.b*deFar;
			smoothness *= smoothness;

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

			float4 terrainLight = tex2D(_TerrainColors, i.tc_Control.xz - reflected.xz*smoothness)*_LightColor0;

			cont.rgb = ( unity_AmbientSky.rgb*terrainLight.a + dott*shadow*0.1+ (terrainLight.rgb *bump.b +pow(max(0.01, dott), 2048* smoothness * 8)* shadow*64)*_LightColor0.rgb) *(1+ dotprod) * 0.5;

			//cont.rgb += pow(bump.b,4)*1024;

			UNITY_APPLY_FOG(i.fogCoord, cont);
			
#if	MODIFY_BRIGHTNESS
			cont.rgb *= _lightControl.a;
#endif

#if COLOR_BLEED
			float3 mix = cont.gbr + cont.brg;
			cont.rgb += mix*mix*_lightControl.r;
#endif


			return cont;

			}


		ENDCG
	}
	  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
}
}
}
