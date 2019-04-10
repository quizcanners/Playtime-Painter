Shader "Playtime Painter/Geometry/Baked Shadows/In Volume" {
	Properties {
		_MainTex_ATL ("Albedo (RGB) (Atlas)", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map (_ATL)", Float) = 0
		[NoScaleOffset]_BumpMapC_ATL ("Combined Map ()", 2D) = "grey" {}
	
		_Microdetail("Microdetail (RG-bump B:Rough A:AO)", 2D) = "white" {}
		[Toggle(VERT_SHADOW)] _vertShad("Has Vertex Shadows", Float) = 0

		[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		[NoScaleOffset]_AtlasTextures("_Textures In Row _ Atlas", float) = 1

	}

	Category{
		Tags{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"RayTrace" = "Opaque"
			"A_VertexColorRole" = "Second Atlas Texture"
			"B_VertexColorRole" = "Additional Wetness"
			"Volume" = "g_BakedShadow_VOL"
		}

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				#pragma shader_feature  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 
				#pragma shader_feature  ___ UV_ATLASED
				#pragma shader_feature  ___ VERT_SHADOW

				uniform sampler2D _MainTex_ATL;
				float4 _MainTex_ATL_ST;
				float4 _MainTex_ATL_TexelSize;

				uniform sampler2D _BumpMapC_ATL;

				uniform sampler2D g_BakedShadow_VOL;
				float4 g_BakedShadows_VOL_TexelSize;

				uniform sampler2D _Microdetail;
			
				float4 _Microdetail_ST;

				float _Glossiness;
				float _AtlasTextures;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 vcol : COLOR0;

					float3 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float2 texcoord2 : TEXCOORD3;
					SHADOW_COORDS(4)
					float3 viewDir: TEXCOORD5;
					float4 edge : TEXCOORD6;
					#if defined(UV_ATLASED)
					float4 atlasedUV : TEXCOORD7;
					float4 atlasedUV2 : TEXCOORD8;
					#endif
					#if !_BUMP_NONE
					float4 wTangent : TEXCOORD9;
					#endif
					#if VERT_SHADOW
					float4 vertShad : TEXCOORD10;
					#endif
					UNITY_FOG_COORDS(11)
				};


				/*
				VolumeBaked shadow recipy:
					texcoord.xw - texture index 0,1
					texcoord1.xy - UV1
					texcoord2 - edge
					texcoord2 - vertex shadow
				*/

				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					UNITY_TRANSFER_FOG(o, o.pos);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
			
					o.vcol = v.color;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					#if !_BUMP_NONE
					o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
					o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
					#endif

					o.texcoord = v.texcoord.xy;
					o.texcoord2 = v.texcoord1.xy;
					o.edge = v.texcoord3;

					TRANSFER_SHADOW(o);

					#if defined(UV_ATLASED)
					vert_atlasedTexture(_AtlasTextures, v.texcoord.z, _MainTex_ATL_TexelSize.x, o.atlasedUV);
					vert_atlasedTexture(_AtlasTextures, v.texcoord.w, _MainTex_ATL_TexelSize.x, o.atlasedUV2);
					#endif

					#if VERT_SHADOW
					o.vertShad = v.texcoord2;
					#endif

					return o;
				}

				float4 frag(v2f i) : COLOR{

					float2 border = DetectEdge(i.edge);
					border.x = max(border.y, border.x);
					float deBorder = 1 - border.x;
					
					#if UV_ATLASED
			
					//	i.texcoord2.xy = (frac(i.texcoord2.xy)*(i.atlasedUV.w) + i.atlasedUV.xy); // for damage
					float2 tc1 = i.texcoord.xy;
					float2 tc2 = i.texcoord.xy;

					float lod;

					float seam = atlasUVlod(tc1, lod, _MainTex_ATL_TexelSize, i.atlasedUV);

					atlasUV(tc2, seam, i.atlasedUV2);

					#if !_BUMP_NONE
					float4 bumpMap = tex2Dlod(_BumpMapC_ATL, float4(tc1, 0, lod));
					float4 bumpMap2 = tex2Dlod(_BumpMapC_ATL, float4(tc2, 0, lod));

					float border2 =  border.x + i.vcol.a;

					border.x = saturate( max(border.x - border.x*deBorder*2, (border2 - 1 +(bumpMap2.b - bumpMap.b) )*16));
					deBorder = 1 - border.x;

					bumpMap = bumpMap * deBorder + bumpMap2 * border.x;

					#endif

					float4 col = tex2Dlod(_MainTex_ATL, float4(tc1, 0, lod)) * deBorder
							+ tex2Dlod(_MainTex_ATL, float4(tc2, 0, lod))*border.x;

					col.a += i.vcol.b*(1 - col.a);

					#else
			
					float2 tc = TRANSFORM_TEX(i.texcoord, _MainTex_ATL);
					float4 col = tex2D(_MainTex_ATL, tc);

					#if !_BUMP_NONE
					float4 bumpMap = tex2D(_BumpMapC_ATL, tc);
					#endif

					col = col * deBorder + i.vcol*border.x;

					#endif

					#if !_BUMP_NONE
					i.texcoord.xy += 0.001 * (bumpMap.rg - 0.5);
					#endif
					float4 micro = tex2D(_Microdetail, TRANSFORM_TEX(i.texcoord.xy, _Microdetail));

					#if !_BUMP_NONE

					float3 tnormal;

					#if _BUMP_REGULAR
					tnormal = UnpackNormal(bumpMap);
					bumpMap = float4(0,0,1,1);
					#else
					bumpMap.rg = (bumpMap.rg - 0.5) * 2;
					tnormal = float3(bumpMap.r, bumpMap.g, 1);
					#endif

					tnormal.rg += (micro.rg - 0.5)*(1-col.a);

					#if !UV_ATLASED
					tnormal = tnormal * deBorder + float3(0, 0, 1)*border.x;
					micro.b = micro.b*deBorder + 0.5*border.x;
					#endif

					applyTangent(i.normal, tnormal,  i.wTangent);

					#else
					float4 bumpMap = float4(0,0,0.5,1);
					#endif

					bumpMap.a *= micro.a;

					#if !UV_ATLASED
					bumpMap.ba = bumpMap.ba*deBorder + float2(1, 1)*border.x;
					#endif

					i.viewDir.xyz = normalize(i.viewDir.xyz);

					i.normal = normalize(i.normal);

					float dotprod = dot(i.viewDir.xyz, i.normal);
					float fernel = (1.5 - dotprod);
					float ambientBlock = (1 - bumpMap.a)*(3+dotprod);
					float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal);
			
					// Point Lights:
			
					float4 bake = SampleVolume(g_BakedShadow_VOL, i.worldPos,  g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, i.normal);

					#if VERT_SHADOW
					bake = max(bake, i.vertShad);
					#endif

					bake = 1 - bake;

					float4 directBake = saturate((bake - 0.5) * 2);

					float power = (pow(col.a, 8*micro.b))*2048;

					power = max(0.001, power);

					float3 scatter = 0;
					float3 glossLight = 0;
					float3 directLight = 0;

					// Point Lights

					PointLight(scatter, glossLight, directLight, i.worldPos.xyz - g_l0pos.xyz,
						i.normal, i.viewDir.xyz, ambientBlock, bake.r, directBake.r, g_l0col, power);

					PointLight(scatter, glossLight, directLight, i.worldPos.xyz - g_l1pos.xyz,
						i.normal, i.viewDir.xyz, ambientBlock, bake.g, directBake.g, g_l1col, power);

					PointLight(scatter, glossLight, directLight, i.worldPos.xyz - g_l2pos.xyz,
						i.normal, i.viewDir.xyz, ambientBlock, bake.b, directBake.b, g_l2col, power);

					glossLight *= 0.1;
					scatter *= (1 - bake.a);

					float shadow = SHADOW_ATTENUATION(i);

					DirectionalLight(scatter, glossLight, directLight,
						shadow*directBake.a, i.normal.xyz, i.viewDir.xyz, ambientBlock, bake.a, power);

					float smoothness = saturate(pow(col.a, 5 - fernel));
					float deDmoothness = 1 - smoothness;

					col.rgb *= (directLight*deDmoothness + (scatter)* bumpMap.a	);

					col.rgb += (glossLight + ShadeSH9(float4(-reflected, 1))*directBake.a)* smoothness;

					BleedAndBrightness(col, 1+shadow*8);

					UNITY_APPLY_FOG(i.fogCoord, col);

					return col;

				}
				ENDCG

			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
		FallBack "Diffuse"
	}

}

