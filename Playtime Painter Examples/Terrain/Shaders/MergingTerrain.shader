Shader "Playtime Painter/Terrain Integration/Terrain Only" {
	Properties{}

	Category{
		Tags { 
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"Queue" = "Geometry"
		}
		
		LOD 200
		ColorMask RGB

		SubShader {
			Pass {

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma multi_compile  ___ _qcPp_WATER_FOAM
				#pragma multi_compile ______ USE_NOISE_TEXTURE

				#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_cg.cginc"

				struct v2f {
					float4 pos : POSITION;
			
					UNITY_FOG_COORDS(1)
					float3 viewDir : TEXCOORD2; 
					float3 wpos : TEXCOORD3;
					float3 tc_Control : TEXCOORD4;
					SHADOW_COORDS(5) 
					float3 normal : TEXCOORD6;
					float2 texcoord : TEXCOORD7;
				};

				v2f vert (appdata_full v) {
					v2f o;

					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.tc_Control.xyz = WORLD_POS_TO_TERRAIN_UV_3D(worldPos.xyz); // -_qcPp_mergeTeraPosition.xyz) / _qcPp_mergeTerrainScale.xyz;

					o.pos = UnityObjectToClipPos(v.vertex);
					
					o.wpos = worldPos;
					o.viewDir.xyz= (WorldSpaceViewDir(v.vertex));
			
					o.texcoord = v.texcoord;

					UNITY_TRANSFER_FOG(o, o.pos);
					TRANSFER_SHADOW(o);

					float3 worldNormal = UnityObjectToWorldNormal(v.normal);

					o.normal =  normalize(worldNormal);

					return o;
				}


				float4 frag (v2f i) : COLOR {
					
					i.viewDir.xyz = normalize(i.viewDir.xyz);

					float4 height = tex2D(_qcPp_mergeTerrainHeight, i.tc_Control.xz + _qcPp_mergeTerrainScale.w);
					float aboveTerrain = saturate((((i.wpos.y - _qcPp_mergeTeraPosition.y) - height.a*_qcPp_mergeTerrainScale.y) - 0.5)*0.5);
					float deAboveTerrain = 1 - aboveTerrain;

					float caustics = 0;

					#if _qcPp_WATER_FOAM
					float underWater = max(0, _qcPp_foamParams.z - i.wpos.y);

					float3 projectedWpos;
					
					float3 nrmNdSm = SAMPLE_WATER_NORMAL(i.viewDir.xyz,  projectedWpos, i.tc_Control, caustics, underWater);

					underWater = min(1, underWater);

					caustics *= underWater;

					#endif
					
					float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz);

					float far = min(1, dist*0.01);
					float deFar = 1 - far;

					float4 col = tex2D(_qcPp_mergeControl, i.tc_Control.xz);

					float3 bump = (height.rgb - 0.5)*2;

					bump = bump*deAboveTerrain + i.normal * aboveTerrain;

					float2 tiled = i.tc_Control.xz*_qcPp_mergeTerrainTiling.xy+ _qcPp_mergeTerrainTiling.zw;
					float tiledY = i.tc_Control.y*_qcPp_mergeTeraPosition.w*2;

					float2 lowtiled = i.tc_Control.xz*_qcPp_mergeTerrainTiling.xy*0.1;

					float4 splaty = tex2D(_qcPp_mergeSplat_4, lowtiled);
					float4 splatz = tex2D(_qcPp_mergeSplat_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplat_4, float2(tiled.x, tiledY))*deFar;
					float4 splatx = tex2D(_qcPp_mergeSplat_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplat_4, float2(tiled.y, tiledY))*deFar;

					// Splat 4 is a base layer:
					float4 splatNy = tex2D(_qcPp_mergeSplatN_4, lowtiled);
					float4 splatNz = tex2D(_qcPp_mergeSplatN_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplatN_4, float2(tiled.x, tiledY))*deFar;
					float4 splatNx = tex2D(_qcPp_mergeSplatN_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplatN_4, float2(tiled.y, tiledY))*deFar;

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

					float ambient = terrainN.a;

					float smoothness = col.a;

					//return caustics;

#if _qcPp_WATER_FOAM
					APPLY_PROJECTED_WATER(underWater, worldNormal, nrmNdSm, i.tc_Control, projectedWpos, i.viewDir.y, col, smoothness, ambient, shadow, caustics);
#endif

					Terrain_Water_AndLight(col, i.tc_Control, 
						ambient, smoothness
						, worldNormal, i.viewDir.xyz,
						shadow , Metallic);

					float4 fogCol = col;
					UNITY_APPLY_FOG(i.fogCoord, fogCol);

					col = APPLY_HEIGHT_FOG(i.wpos.y, col, fogCol);

					BleedAndBrightness(col, 1, i.texcoord.xy*10000);

					return col;
				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
	}
}
