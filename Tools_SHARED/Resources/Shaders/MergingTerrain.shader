// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Terrain/MergingTerrain" {

	Category{
			Tags { "RenderType" = "Opaque"
			 "LightMode" = "ForwardBase"
			   "Queue" = "Geometry"
			 }
				LOD 200
			ColorMask RGBA


	SubShader {
		Pass {



		CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
		#include "UnityLightingCommon.cginc" 
			 #include "Lighting.cginc"
			//#include "UnityCG.cginc"
			 #include "AutoLight.cginc"
		#include "VertexDataProcessInclude.cginc"

		#pragma multi_compile_fwdbase //nolightmap nodirlightmap nodynlightmap novertexlight

		#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
		#pragma multi_compile  ___ COLOR_BLEED
	

			struct v2f {
				float4 pos : POSITION;
			
				UNITY_FOG_COORDS(1)
				float3 viewDir : TEXCOORD2; 
				float3 wpos : TEXCOORD3;
				float3 tc_Control : TEXCOORD4;
				float3 fwpos : TEXCOORD5;
				SHADOW_COORDS(6) 
				float3 normal : TEXCOORD7;
				float2 texcoord : TEXCOORD8;
			};

			v2f vert (appdata_full v) {
				v2f o;

				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.tc_Control.xyz = (worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;
			//	o.tc_Control.y = (worldPos.y - _mergeTeraPosition.y) / _mergeTerrainScale.xz;


				// The portion below is to preview editing.
			/*	float4 height = tex2Dlod(_mergeTerrainHeight, float4(o.tc_Control.xy, 0, 0));
				worldPos.y = _mergeTeraPosition.y + height.a*_mergeTerrainScale.y;
				v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz, v.vertex.w));*/
				// end of preview, can be commented out for build


				o.pos = UnityObjectToClipPos(v.vertex);
				o.wpos = worldPos;
				o.viewDir.xyz= (WorldSpaceViewDir(v.vertex));
			
				o.texcoord = v.texcoord;
				UNITY_TRANSFER_FOG(o, o.pos);
				TRANSFER_SHADOW(o);

				float3 worldNormal = UnityObjectToWorldNormal(v.normal);

				o.normal =  normalize(worldNormal);

				o.fwpos = foamStuff(o.wpos);
		

				return o;
			}


			float4 frag (v2f i) : COLOR {
				i.viewDir.xyz = normalize(i.viewDir.xyz);
			float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz);

			float far = min(1, dist*0.01);
			float deFar = 1- far;


			float4 cont = tex2D(_mergeControl, i.tc_Control.xz);
			float4 height = tex2D(_mergeTerrainHeight, i.tc_Control.xz + _mergeTerrainScale.w);
			float3 bump = (height.rgb - 0.5)*2;

			float aboveTerrain = saturate((((i.wpos.y - _mergeTeraPosition.y) - height.a*_mergeTerrainScale.y) - 0.5)*0.5);
			float deAboveTerrain = 1 - aboveTerrain;


			bump = bump*deAboveTerrain + i.normal * aboveTerrain;


			float2 tiled = i.tc_Control.xz*_mergeTerrainTiling.xy+ _mergeTerrainTiling.zw;
			float tiledY = i.tc_Control.y*_mergeTeraPosition.w*2;

			float2 lowtiled = i.tc_Control.xz*_mergeTerrainTiling.xy*0.1;

			float4 splat0 = tex2D(_mergeSplat_0, lowtiled)*far + tex2D(_mergeSplat_0, tiled)*deFar;
			float4 splat1 = tex2D(_mergeSplat_1, lowtiled)*far + tex2D(_mergeSplat_1, tiled)*deFar;
			float4 splat2 = tex2D(_mergeSplat_2, lowtiled)*far + tex2D(_mergeSplat_2, tiled)*deFar;
			float4 splat3 = tex2D(_mergeSplat_3, lowtiled)*far + tex2D(_mergeSplat_3, tiled)*deFar;
			
			float4 splaty = tex2D(_mergeSplat_4, lowtiled);//*far + tex2D(_mergeSplat_4, tiled)	*deFar;
			float4 splatz = tex2D(_mergeSplat_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_mergeSplat_4, float2(tiled.x, tiledY))*deFar;
			float4 splatx = tex2D(_mergeSplat_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_mergeSplat_4, float2(tiled.y, tiledY))*deFar;

			float4 splat0N = tex2D(_mergeSplatN_0, lowtiled)*far + tex2D(_mergeSplatN_0, tiled)*deFar;
			float4 splat1N = tex2D(_mergeSplatN_1, lowtiled)*far + tex2D(_mergeSplatN_1, tiled)*deFar;
			float4 splat2N = tex2D(_mergeSplatN_2, lowtiled)*far + tex2D(_mergeSplatN_2, tiled)*deFar;
			float4 splat3N = tex2D(_mergeSplatN_3, lowtiled)*far + tex2D(_mergeSplatN_3, tiled)*deFar;

			// Splat 4 is a base layer:
			float4 splatNy = tex2D(_mergeSplatN_4, lowtiled);//*far + tex2D(_mergeSplatN_4, tiled)*deFar;
			float4 splatNz = tex2D(_mergeSplatN_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_mergeSplatN_4, float2(tiled.x, tiledY))*deFar;
			float4 splatNx = tex2D(_mergeSplatN_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_mergeSplatN_4, float2(tiled.y, tiledY))*deFar;
		


			float edge = MERGE_POWER;

			

			float4 terrain = splaty;
			float4 terrainN = splatNy;


			float maxheight = (1+splaty.a)*abs(bump.y);

			float3 newBump = float3(splatNy.x-0.5,0.33, splatNy.y-0.5);

			//Triplanar X:
			float newHeight = (1.5 + splatx.a)*abs(bump.x);
			float adiff = max(0, (newHeight - maxheight));
			float alpha = min(1, adiff*(1 + edge*terrainN.b*splatNx.b));
			float dAlpha = (1 - alpha);
			terrain = terrain*dAlpha + splatx*alpha;
			terrainN.ba = terrainN.ba*dAlpha + splatNx.ba*alpha;
			newBump = newBump*dAlpha + float3(0, splatNx.y - 0.5,splatNx.x-0.5)*alpha;
			maxheight += adiff;

			//Triplanar Z:
			newHeight = (1.5 + splatz.a)*abs(bump.z);
			adiff = max(0, (newHeight - maxheight));
			alpha = min(1, adiff*(1 + edge*terrainN.b*splatNz.b));
			dAlpha = (1 - alpha);
			terrain = terrain*(dAlpha) +splatz*alpha;
			terrainN.ba = terrainN.ba*dAlpha + splatNz.ba*alpha;
			newBump = newBump*dAlpha + float3(splatNz.x - 0.5, splatNz.y - 0.5, 0)*alpha;
			maxheight += adiff;

			terrainN.rg = 0.5;

			float tripMaxH = maxheight;
			float3 tmpbump = normalize(bump+ newBump*2);

			float triplanarY = max(0, tmpbump.y)*2; // Recalculate it based on previously sampled bump

			newHeight = cont.r * triplanarY + splat0.a;
			adiff = max(0, (newHeight - maxheight));
			alpha = min(1, adiff*(1 + edge*terrainN.b*splat0N.b));
			dAlpha = (1 - alpha);
			terrain = terrain*(dAlpha) + splat0*alpha;
			terrainN = terrainN*(dAlpha) + splat0N*alpha;
			maxheight += adiff;


			newHeight = cont.g*triplanarY +splat1.a;
			adiff = max(0, (newHeight-maxheight));
			alpha = min(1,adiff*(1 + edge*terrainN.b*splat1N.b));
			dAlpha = (1 - alpha);
			terrain = terrain*(dAlpha)+splat1*alpha;
			terrainN = terrainN*(dAlpha)+splat1N*alpha;
			maxheight += adiff;
			

			newHeight = cont.b*triplanarY +splat2.a;
			adiff = max(0, (newHeight-maxheight));
			alpha = min(1,adiff*(1 + edge*terrainN.b*splat2N.b));
			dAlpha = (1 - alpha);
			terrain = terrain*(dAlpha)+splat2*alpha;
			terrainN = terrainN*(dAlpha)+splat2N*alpha;
			maxheight += adiff;

			newHeight = cont.a*triplanarY +splat3.a;
			adiff = max(0, (newHeight-maxheight));
			alpha = min(1,adiff*(1 + edge*terrainN.b*splat3N.b));
			dAlpha = (1 - alpha);
			terrain = terrain*(dAlpha)+splat3*alpha;
			terrainN = terrainN*(dAlpha)+splat3N*alpha;
			maxheight += adiff;
		
			//terrain.a = maxheight*0.3;  // new

				terrainN.rg = terrainN.rg*2 -1;

				adiff = max(0, (tripMaxH+0.5- maxheight));
				alpha = min(1, adiff*2);

				bump = tmpbump*alpha + (1 - alpha)*bump;

				cont = terrain;
		
			float wetSection = saturate(_foamParams.w-i.fwpos.y- (cont.a)*_foamParams.w)*(1 - terrainN.b);
			i.fwpos.y += cont.a; 
			

			float3 worldNormal = normalize(bump +float3(terrainN.r, 0, terrainN.g));

			float dotprod = max(0,dot(worldNormal,  i.viewDir.xyz));
			float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*worldNormal);

			/*float l = cos(_foamParams.x+i.fwpos.x) -i.fwpos.y;
			float dl = max(0,0.2-abs(l));

			float l1 = sin(_foamParams.y+i.fwpos.z) -i.fwpos.y;
			float dl1 = max(0,(0.3-abs(l1))*max(0,1-l));

			float foamAlpha = (dl+dl1);
			float foamWhite;
			foamWhite = saturate(max(l,l1)*8);*/


			float2 foamA_W = foamAlphaWhite(i.fwpos);
			float water = max(0.5, min(i.fwpos.y + 2 - (foamA_W.x) * 2, 1)); // MODIFIED
			float under = (water - 0.5) * 2;
			
			terrainN.b =  max(terrainN.b, wetSection*under); // MODIFIED

			float fernel = 1.5 - dotprod;

			//terrainN.b*=tex2D(_MicroDetail, i.texcoord.xy * 4 * (aboveTerrain)+(i.tc_Control.xz) * 128 * deAboveTerrain).a;//*deAboveTerrain + aboveTerrain;

			float smoothness = (pow(terrainN.b, (3-fernel)*2));  //terrainN.b*terrainN.b;//+((1 - dotprod)*(1 - terrainN.b)));
			float deSmoothness = (1 - smoothness);

			float ambientBlock =  (1 - terrainN.a)*dotprod; // MODIFIED


			float shadow = saturate((SHADOW_ATTENUATION(i) * 2 - ambientBlock));

			float3 teraBounce = _LightColor0.rgb*TERABOUNCE;
			float4 terrainAmbient = tex2D(_TerrainColors, i.tc_Control.xz);
			terrainAmbient.rgb *= teraBounce;
			terrainAmbient.a *= terrainN.a;

			float4 terrainLight = tex2D(_TerrainColors, i.tc_Control.xz - reflected.xz*terrainN.b*terrainAmbient.a*0.1);
			terrainLight.rgb*= teraBounce;
		
			
			float diff = dot(worldNormal, _WorldSpaceLightPos0.xyz);
			diff = saturate(diff - ambientBlock*4*(1- diff));

			float direct = diff*shadow;

			//

			float3 ambientSky = (unity_AmbientSky.rgb * max(0, worldNormal.y - 0.5) * 2)*terrainAmbient.a;

			float4 col;

			col.a = water; // NEW

			col.rgb = (cont.rgb* (_LightColor0*direct + (ambientSky + terrainAmbient.rgb
				
				)*fernel)*deSmoothness*terrainAmbient.a + foamA_W.y*(0.5+shadow)*(under) // MODIFIED
				
				);
			
			float power = 
				smoothness *1024;// / dist;

			float up = saturate((-reflected.y - 0.5) * 2 * terrainLight.a);//;

			float3 reflResult = (
				((pow(max(0.01, dot(_WorldSpaceLightPos0, -reflected)), power)* direct	*(_LightColor0)*power)) +

				terrainLight.rgb*(1 - up) +
				unity_AmbientSky.rgb *up//*terrainAmbient.a

				)* terrainN.b * fernel;

			col.rgb += reflResult*( under);
			
			col.rgb *= 1-saturate ((_foamParams.z - i.wpos.y)*0.1);  // NEW
		

			float4 fogged = col;
			UNITY_APPLY_FOG(i.fogCoord, fogged);
			float fogging = (32-max(0,i.wpos.y-_foamParams.z))/32;

			fogging = min(1,pow(max(0,fogging),2));
			col.rgb = fogged.rgb * fogging + col.rgb *(1-fogging);



			#if	MODIFY_BRIGHTNESS
			col.rgb *= _lightControl.a;
#endif

			#if COLOR_BLEED
			float3 mix = col.gbr + col.brg;
			col.rgb += mix*mix*_lightControl.r;
#endif


//col.rgb = worldNormal;
			//col.rgb = bump.rgb;
			//col.rgb = ambientSky.rgb;
			//col.rg = abs(reflected.xz);
			//col.b = 0;
			//terrainLight.rgb *= cont.rgb;
			return
				//aboveTerrain;
				//ambientPower;
				//micro;
				//power;//
				//terrainLight;//*(1 - smoothness);
				//smoothness;
				//cont;
				//power;
				//splat0N;
				//terrainAmbient.a;
				//fernel;
				//shadow;
				//diff;
				//up;
				//deSmoothness;
				//i.tc_Control.y;
				col;//+aboveTerrain*0.5;
			//dotprod;
				//terrainAmbient;
			}


		ENDCG
	}
	  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
}
}
}
