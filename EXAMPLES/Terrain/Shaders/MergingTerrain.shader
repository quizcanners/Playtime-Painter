Shader "Playtime Painter/Terrain Integration/Terrain Only" {

	Category{
		Tags { 
			"RenderType" = "Opaque"
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

				#include "Assets/Tools/quizcanners/VertexDataProcessInclude.cginc"

				#pragma multi_compile_fwdbase

				#pragma multi_compile  ___ WATER_FOAM

				struct v2f {
					float4 pos : POSITION;
			
					UNITY_FOG_COORDS(1)
					float3 viewDir : TEXCOORD2; 
					float3 wpos : TEXCOORD3;
					float3 tc_Control : TEXCOORD4;
					#if WATER_FOAM
					float4 fwpos : TEXCOORD5;
					#endif
					SHADOW_COORDS(6) 
					float3 normal : TEXCOORD7;
					float2 texcoord : TEXCOORD8;
				};

				v2f vert (appdata_full v) {
					v2f o;

					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.tc_Control.xyz = (worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;

					o.pos = UnityObjectToClipPos(v.vertex);
					o.wpos = worldPos;
					o.viewDir.xyz= (WorldSpaceViewDir(v.vertex));
			
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o, o.pos);
					TRANSFER_SHADOW(o);

					float3 worldNormal = UnityObjectToWorldNormal(v.normal);

					o.normal =  normalize(worldNormal);

					#if WATER_FOAM
					o.fwpos = foamStuff(o.wpos);
					#endif

					return o;
				}


				float4 frag (v2f i) : COLOR {
					
					i.viewDir.xyz = normalize(i.viewDir.xyz);
					float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz);

					float far = min(1, dist*0.01);
					float deFar = 1 - far;

					float4 col = tex2D(_mergeControl, i.tc_Control.xz);
					float4 height = tex2D(_mergeTerrainHeight, i.tc_Control.xz + _mergeTerrainScale.w);
					float3 bump = (height.rgb - 0.5)*2;

					float aboveTerrain = saturate((((i.wpos.y - _mergeTeraPosition.y) - height.a*_mergeTerrainScale.y) - 0.5)*0.5);
					float deAboveTerrain = 1 - aboveTerrain;

					bump = bump*deAboveTerrain + i.normal * aboveTerrain;

					float2 tiled = i.tc_Control.xz*_mergeTerrainTiling.xy+ _mergeTerrainTiling.zw;
					float tiledY = i.tc_Control.y*_mergeTeraPosition.w*2;

					float2 lowtiled = i.tc_Control.xz*_mergeTerrainTiling.xy*0.1;

					float4 splaty = tex2D(_mergeSplat_4, lowtiled);
					float4 splatz = tex2D(_mergeSplat_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_mergeSplat_4, float2(tiled.x, tiledY))*deFar;
					float4 splatx = tex2D(_mergeSplat_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_mergeSplat_4, float2(tiled.y, tiledY))*deFar;

					// Splat 4 is a base layer:
					float4 splatNy = tex2D(_mergeSplatN_4, lowtiled);
					float4 splatNz = tex2D(_mergeSplatN_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_mergeSplatN_4, float2(tiled.x, tiledY))*deFar;
					float4 splatNx = tex2D(_mergeSplatN_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_mergeSplatN_4, float2(tiled.y, tiledY))*deFar;

					float edge = MERGE_POWER;

					float4 terrain = splaty;
					float4 terrainN = splatNy;

					float maxheight = (1+splatNy.b)*abs(bump.y);

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

					Terrain_4_Splats(col, lowtiled, tiled, far, deFar, terrain, triplanarY, terrainN, maxheight);

					adiff = max(0, (tripMaxH + 0.5 - maxheight));
					alpha = min(1, adiff * 2);

					bump = tmpbump*alpha + (1 - alpha)*bump;

					float3 worldNormal = normalize(bump + float3(terrainN.r, 0, terrainN.g));

					col = terrain;
	
					float shadow = SHADOW_ATTENUATION(i);

					float Metallic = 0;

					Terrain_Light(i.tc_Control, terrainN, worldNormal, i.viewDir.xyz, col, shadow , Metallic, 
					#if WATER_FOAM
					i.fwpos
					#else
					0
					#endif
					);

					UNITY_APPLY_FOG(i.fogCoord, col);

					return col;
				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
	}
}
