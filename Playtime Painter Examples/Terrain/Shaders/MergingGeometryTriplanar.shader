Shader "Playtime Painter/Terrain Integration/Triplanar" {
	Properties{
		[NoScaleOffset]_MainTex("Geometry Texture (RGB)", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Map", Float) = 0
		[NoScaleOffset]_Map("Geometry Combined Maps (RGBA)", 2D) = "gray" {}
		_Merge("_Merge", Range(0.01,2)) = 1
		[Toggle(CLIP_ALPHA)] _ALPHA("Clip Alpha", Float) = 0
	}
    
	Category{
		Tags{
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"Queue" = "Geometry"
		}
		LOD 200
		ColorMask RGBA

		SubShader{
			Pass{

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog

				#include "qc_terrain_cg.cginc"

				#pragma multi_compile_fwdbase 
				#pragma shader_feature  ___ _BUMP_NONE  _BUMP_COMBINED 
				#pragma shader_feature  ___ CLIP_ALPHA
				//#pragma multi_compile ______ USE_NOISE_TEXTURE

				sampler2D _MainTex;
				sampler2D _Map;

				struct v2f {
					float4 pos : POSITION;

					UNITY_FOG_COORDS(1)
					float3 viewDir : TEXCOORD2;
					float3 wpos : TEXCOORD3;
					float3 tc_Control : TEXCOORD4;
					SHADOW_COORDS(5)
					float2 texcoord : TEXCOORD6;
					#if _BUMP_NONE
					float3 normal : TEXCOORD7;
					#else
					float3 tspace0 : TEXCOORD7; 
					float3 tspace1 : TEXCOORD8; 
					float3 tspace2 : TEXCOORD9; 
					#endif
				};

				v2f vert(appdata_full v) {
					v2f o;

					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.tc_Control.xyz = (worldPos.xyz - _qcPp_mergeTeraPosition.xyz) / _qcPp_mergeTerrainScale.xyz;

					o.pos = UnityObjectToClipPos(v.vertex);
					o.wpos = worldPos;
					o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));

					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o, o.pos);
					TRANSFER_SHADOW(o);

					float3 worldNormal = UnityObjectToWorldNormal(v.normal);

					half3 wNormal = worldNormal;

					#if _BUMP_NONE
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
					#else
					half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
					half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					half3 wBitangent = cross(wNormal, wTangent) * tangentSign;

					o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
					o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
					o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
					#endif

					return o;
				}

				float4 frag(v2f i) : COLOR{
					i.viewDir.xyz = normalize(i.viewDir.xyz);
					float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz);
					float caustics = 0;
					/*
#if _qcPp_WATER_FOAM
					float underWater = _qcPp_foamParams.z - i.wpos.y;
					float3 projectedWpos;
						
					float3 nrmNdSm = SAMPLE_WATER_NORMAL(i.viewDir.xyz,  projectedWpos, i.tc_Control, caustics, underWater);
#endif*/

					float4 col = tex2D(_MainTex, i.texcoord.xy);
	
					#if CLIP_ALPHA
						clip(col.a - 0.5);
						col.a = 0.1;
					#endif

					#if _BUMP_NONE
						float3 worldNormal = i.normal;
						float4 bumpMap = float4(0, 0, 1, 1);
					#else

					float4 bumpMap = tex2D(_Map, i.texcoord.xy);
					float3 tnormal;
				#if _BUMP_REGULAR
					tnormal = UnpackNormal(bumpMap);
					bumpMap = float4(0, 0, 1, 1);
				#else
					bumpMap.rg = (bumpMap.rg - 0.5) * 2;
					tnormal = float3(bumpMap.r, bumpMap.g, 1);
				#endif

					float3 worldNormal;
					worldNormal.x = dot(i.tspace0, tnormal);
					worldNormal.y = dot(i.tspace1, tnormal);
					worldNormal.z = dot(i.tspace2, tnormal);
					#endif

					float4 terrainN = 0;

					Terrain_Trilanear(i.tc_Control, i.wpos, dist, worldNormal, col, terrainN, bumpMap);

					float shadow = SHADOW_ATTENUATION(i);

					float Metalic = 0;

					float ambient = terrainN.a;

					float smoothness = col.a;
				

/*#if _qcPp_WATER_FOAM
					APPLY_PROJECTED_WATER(saturate(underWater), worldNormal, nrmNdSm, i.tc_Control, projectedWpos, i.viewDir.y, col, smoothness, ambient, shadow);
#endif
*/

					Terrain_Water_AndLight(col, i.tc_Control, ambient, smoothness, worldNormal, i.viewDir.xyz,  shadow);

					float4 fogCol = col;
					UNITY_APPLY_FOG(i.fogCoord, fogCol);

					col = APPLY_HEIGHT_FOG(i.wpos.y, col, fogCol);

					BleedAndBrightness(col, 1, i.texcoord.xy);

					return col;
				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
	}
}
