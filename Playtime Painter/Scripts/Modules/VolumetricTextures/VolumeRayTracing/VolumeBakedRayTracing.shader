Shader "Playtime Painter/Geometry/Ray Tracing/Simple" {
	Properties {
		_MainTex_ATL("Albedo (RGB) (Atlas)", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map (_ATL)", Float) = 0
		[NoScaleOffset]_BumpMapC_ATL("Combined Map ()", 2D) = "grey" {}

		_Microdetail("Microdetail (RG-bump B:Rough A:AO)", 2D) = "white" {}

		[Toggle(_qcPp_UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		_qcPp_AtlasTextures("_Textures In Row _ Atlas", float) = 1

		_Test ("Test", Range(0,1)) = 1
	}

	Category{
		Tags{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
		}

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_fog
				#pragma shader_feature  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 
				#pragma shader_feature  ___ _qcPp_UV_ATLASED
				#pragma multi_compile ______ USE_NOISE_TEXTURE
				#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_cg.cginc"


				uniform sampler2D _MainTex_ATL;
				uniform sampler2D _Microdetail;
				float4 _MainTex_ATL_ST;
				float4 _MainTex_ATL_TexelSize;
				uniform sampler2D _BumpMapC_ATL;
				float4 _Microdetail_ST;
				float _qcPp_AtlasTextures;
				float _Test;

				float4 g_BakedRays_VOLVOLUME_H_SLICES; //g_VOLUME_H_SLICES;
				float4 g_BakedRays_VOLVOLUME_POSITION_N_SIZE; //g_VOLUME_POSITION_N_SIZE;

				uniform sampler2D g_BakedRays_VOL;
				float4 g_BakedRays_VOL_TexelSize;

				float4 g_l0pos;
				float4 g_l0col;
				float4 g_l1pos;
				float4 g_l1col;
				float4 g_l2pos;
				float4 g_l2col;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 vcol : COLOR0;

					float4 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					SHADOW_COORDS(3)
					float3 viewDir: TEXCOORD4;
					float4 edge : TEXCOORD5;
					UNITY_FOG_COORDS(6)
					//float4 shadowCoords0 : TEXCOORD7; // Replacing R with directional light
					float4 shadowCoords1 : TEXCOORD8;
					float4 shadowCoords2 : TEXCOORD9;
#if defined(_qcPp_UV_ATLASED)
					float4 atlasedUV : TEXCOORD10;
					float4 atlasedUV2 : TEXCOORD11;
#endif

#if !_BUMP_NONE
					float4 wTangent : TEXCOORD12;
#endif
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					UNITY_TRANSFER_FOG(o, o.pos);
					o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0f));
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
			
					o.vcol = v.color;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					o.texcoord = v.texcoord.xy;
					o.edge = v.texcoord3;

					TRANSFER_SHADOW(o);
					
					o.shadowCoords1 = mul(rt1_ProjectorMatrix, o.worldPos);
					o.shadowCoords2 = mul(rt2_ProjectorMatrix, o.worldPos);

					#if defined(_qcPp_UV_ATLASED)
					vert_atlasedTexture(_qcPp_AtlasTextures, v.texcoord.z, _MainTex_ATL_TexelSize.x, o.atlasedUV);
					vert_atlasedTexture(_qcPp_AtlasTextures, v.texcoord.w, _MainTex_ATL_TexelSize.x, o.atlasedUV2);
					#endif

					#if !_BUMP_NONE
					o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
					o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
					#endif

					return o;
				}

				inline void PointLightTrace(inout float3 scatter, inout float3 glossLight, inout float3 directLight,
					float3 vec, float3 normal, float3 viewDir, float bake, float shadow, float4 lcol, float power, float ambientBlock, float smoothness) {

					float len = length(vec);
					vec /= len;

					float direct = max(0, dot(normal, -vec));
					
					ambientBlock = 0;

					direct = saturate(direct - (ambientBlock)*4*(1-direct));

					float3 halfDirection = normalize(viewDir - vec);
					float NdotH = max(0.01, (dot(normal, halfDirection)));
					float normTerm = pow(NdotH, power); 

					scatter += bake * lcol.rgb;

					lcol.rgb *= direct* shadow;

					glossLight += lcol.rgb*normTerm;
					directLight += lcol.rgb / (len * len) ;
				}

			

				float4 frag(v2f o) : COLOR{

					o.normal = normalize(o.normal);

					o.viewDir.xyz = normalize(o.viewDir.xyz);

					float ambientBlock = max(0, dot(o.normal.xyz, o.viewDir.xyz));

					float2 border = DetectEdge(o.edge);
					border.x = max(border.y, border.x);
					float deBorder = 1 - border.x;

#if _qcPp_UV_ATLASED

					float2 tc1 = o.texcoord.xy;
					float2 tc2 = o.texcoord.xy;

					float lod;

					float seam = atlasUVlod(tc1, lod, _MainTex_ATL_TexelSize, o.atlasedUV);

					atlasUV(tc2, seam, o.atlasedUV2);

#if !_BUMP_NONE
					float4 bumpMap = tex2Dlod(_BumpMapC_ATL, float4(tc1, 0, lod));
					float4 bumpMap2 = tex2Dlod(_BumpMapC_ATL, float4(tc2, 0, lod));

					float border2 = border.x + o.vcol.a;

					border.x = saturate(max(border.x - border.x*deBorder * 2, (border2 - 1 + (bumpMap2.b - bumpMap.b)) * 16));
					deBorder = 1 - border.x;

					bumpMap = bumpMap * deBorder + bumpMap2 * border.x;

#endif

					float4 col = tex2Dlod(_MainTex_ATL, float4(tc1, 0, lod)) * deBorder
						+ tex2Dlod(_MainTex_ATL, float4(tc2, 0, lod))*border.x;

					col.a += o.vcol.b*(1 - col.a);

#else

					float2 tc = TRANSFORM_TEX(o.texcoord, _MainTex_ATL);
					float4 col = tex2D(_MainTex_ATL, tc);

#if !_BUMP_NONE
					float4 bumpMap = tex2D(_BumpMapC_ATL, tc);
#endif

					col = col * deBorder + o.vcol*border.x;

#endif

#if !_BUMP_NONE
					o.texcoord.xy += 0.001 * (bumpMap.rg - 0.5);
#endif
					float4 micro = tex2D(_Microdetail, TRANSFORM_TEX(o.texcoord.xy, _Microdetail));

#if !_BUMP_NONE

					float3 tnormal;

#if _BUMP_REGULAR
					tnormal = UnpackNormal(bumpMap);
					bumpMap = float4(0, 0, 1, 1);
#else
					bumpMap.rg = (bumpMap.rg - 0.5) * 2;
					tnormal = float3(bumpMap.r, bumpMap.g, 1);
#endif

					tnormal.rg += (micro.rg - 0.5)*(1 - col.a);

#if !_qcPp_UV_ATLASED
					tnormal = tnormal * deBorder + float3(0, 0, 1)*border.x;
					micro.b = micro.b*deBorder + 0.5*border.x;
#endif

					applyTangent(o.normal, tnormal, o.wTangent);

#else
					float4 bumpMap = float4(0, 0, 0.5, 1);
#endif

					bumpMap.a *= micro.a;

#if !_qcPp_UV_ATLASED
					bumpMap.ba = bumpMap.ba*deBorder + float2(1, 1)*border.x;
#endif

					float dotprod = dot(o.viewDir.xyz, o.normal);
					float fernel = (1.5 - dotprod);

					float ambient = bumpMap.a;
					
					ambientBlock *= (1 - ambient);

					float smoothness = saturate(pow(col.a, 5 - fernel));
					float deDmoothness = 1 - smoothness;

					// Ray Tracing part

					float shad_G = GetRayTracedShadows(o.worldPos.xyz, o.normal, o.shadowCoords1,
						rt1_ProjectorConfiguration, rt1_ProjectorClipPrecompute, rt1_ProjectorPosition, float4(0, 1, 0, 0));

					float3 reflected = normalize(o.viewDir.xyz - 2 * (dotprod)*o.normal);
						
					//float4 g_BakedRays_VOLVOLUME_H_SLICES; //g_VOLUME_H_SLICES;
					//float4 g_BakedRays_VOLVOLUME_POSITION_N_SIZE; //g_VOLUME_POSITION_N_SIZE;


					float4 bake = SampleVolume(g_BakedRays_VOL, o.worldPos.xyz + o.normal - reflected * fernel * (0.25 + smoothness), 
						g_BakedRays_VOLVOLUME_POSITION_N_SIZE,
						g_BakedRays_VOLVOLUME_H_SLICES);

					float power = smoothness * (128+ fernel*128);

					float3 scatter = 0;
					float3 glossLight = 0;
					float3 directLight = 0;

					float shadow = SHADOW_ATTENUATION(o);


					// Directional sun/Moon
					//PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l0pos.xyz,
						//o.normal, o.viewDir.xyz, bake.r, shadow,  g_l0col, power, ambientBlock, smoothness);

					PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l1pos.xyz,
						o.normal, o.viewDir.xyz, bake.g, shad_G,  g_l1col, power, ambientBlock, smoothness);

					scatter *= 0.01;

					//Ambient
					//PointLightTrace(scatter, glossLight, directLight, o.worldPos.xyz - g_l2pos.xyz,
						//o.normal, o.viewDir.xyz, bake.b, 0,  g_l2col, power, ambientBlock, smoothness);

					//return bake;
					
					scatter += unity_AmbientSky.rgb * bake.b
#if !UNITY_COLORSPACE_GAMMA
						*16
#endif
						
						;

					// Sun light
					float diff = saturate((dot(o.normal, _WorldSpaceLightPos0.xyz)));
					diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	
					float3 directSun = _LightColor0.rgb * diff * shadow * (1 - bake.a);



					directLight += directSun;
					
					scatter += _LightColor0.rgb * bake.r * 100;

					float3 halfDirection = normalize(o.viewDir.xyz + _WorldSpaceLightPos0.xyz);
					float NdotH = max(0.01, (dot(o.normal.xyz, halfDirection)));
					float normTerm = pow(NdotH, power)*power;

					glossLight += normTerm * directSun;

					float glossy = col.a;

					col.rgb *= (directLight + scatter * ambient) * (1- glossy);

					col.rgb += (glossLight*glossy
						+ (scatter*fernel)    // Every surface has a bit of glossy reflection, this part simulates it
					#if !UNITY_COLORSPACE_GAMMA
					* 0.1
					#endif
						) * 0.002;

				

					

					float4 fogCol = col;
					UNITY_APPLY_FOG(o.fogCoord, fogCol);

					col = APPLY_HEIGHT_FOG(o.worldPos.y, col, fogCol);

					BleedAndBrightness(col, 1, o.texcoord.xy);


					return  col;

				}
				ENDCG

			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
		FallBack "Diffuse"
	}
}

