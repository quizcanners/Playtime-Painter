
Shader "Playtime Painter/Geometry/Destructible/Character" {
	Properties{
		[NoScaleOffset]_MainTex("Damage Mask", 2D) = "black" {}
		[NoScaleOffset]_Diffuse("Main", 2D) = "white" {}
		[NoScaleOffset]_NrmyM("Main Combined Map", 2D) = "gray" {}
		[NoScaleOffset]_Dirt("Dirt (RGB)", 2D) = "white" {}
		[NoScaleOffset]_MainTexScratch("Scratch Diffuse (RGB)", 2D) = "white" {}
		[NoScaleOffset]_NrmyScratch("Scratch Combined Map", 2D) = "gray" {}
		[NoScaleOffset]_MainTexDam("Damage Diffuse", 2D) = "white" {}
		[NoScaleOffset]_NrmyDam("Damage Combined Map", 2D) = "gray" {}
		[Toggle(_DEBUG_UV2)] debugUV2("Debug UV2", Float) = 0
	}


	Category{
		Tags{ 
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"Queue" = "Geometry"
		}

		LOD 200
		ColorMask RGB


		SubShader{
			Pass{

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_built_in.cginc"

				#pragma multi_compile_fwdbase 
				#pragma shader_feature __ _DEBUG_UV2

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;
				sampler2D _NrmyM;
				sampler2D _MainTexDam;
				sampler2D _MainTexScratch;
				sampler2D _NrmyDam;
				sampler2D _NrmyScratch;
				sampler2D _Dirt;
				sampler2D _Diffuse;

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;
					float2 texcoord2 : TEXCOORD1;
					float3 viewDir : TEXCOORD2;
					float3 wpos :	TEXCOORD3;
					half3 tspace0 : TEXCOORD4; 
					half3 tspace1 : TEXCOORD5; 
					half3 tspace2 : TEXCOORD6; 
				};

				v2f vert(appdata_full v) {
					v2f o;

					float3 worldNormal = UnityObjectToWorldNormal(v.normal);

					float4 mask = tex2Dlod(_MainTex, float4(v.texcoord2.xy,0,0));

					v.vertex.xyz -= (v.normal)*0.05*pow(mask.r,3);

					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	
					o.pos = UnityObjectToClipPos(v.vertex);
					o.wpos = worldPos;
					o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));

					o.texcoord = v.texcoord;

					o.texcoord2 = v.texcoord1.xy;

					half3 wNormal = worldNormal;
					half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
		
					half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
					half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
		
					o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
					o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
					o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);

					return o;
				}


				float4 frag(v2f i) : COLOR{

					i.viewDir.xyz = normalize(i.viewDir.xyz);

					float4 geocol = tex2D(_Diffuse, i.texcoord.xy);
					float4 bumpMap = tex2D(_NrmyM, i.texcoord.xy);
					float4 blood = tex2D(_MainTexDam, i.texcoord.xy);
					float4 scratch = tex2D(_MainTexScratch, i.texcoord.xy*8);
					float4 dirt = tex2D(_Dirt, i.texcoord.xy*8);

					float2 damageUV = i.texcoord2.xy;//i.texcoord2.xy;

					float4 mask = tex2D(_MainTex, damageUV);

#if _DEBUG_UV2
					return float4(damageUV, mask.r,dirt.r); //mask;
#endif

					float maskr = tex2D(_MainTex, float2(i.texcoord.x, i.texcoord.y + _MainTex_TexelSize.y)).r;
					float maskg = tex2D(_MainTex, float2(i.texcoord.x + _MainTex_TexelSize.x, i.texcoord.y)).r;

					float bloodAmount = (mask.r*8 + scratch.a - 1 - geocol.a);

					float scratchAmount = saturate((1+ scratch.a)*3 - bloodAmount*(1 + blood.a));
					float deScratch = 1 - scratchAmount;
					blood = scratch*scratchAmount + blood*(1 - scratchAmount);

					bloodAmount = saturate(bloodAmount);
					float deBlood = 1 - bloodAmount;

					geocol = geocol*(deBlood)+blood*bloodAmount;
					bumpMap = bumpMap*(deBlood)+(tex2D(_NrmyScratch, i.texcoord.xy*8)*scratchAmount + tex2D(_NrmyDam, i.texcoord.xy)*deScratch)*bloodAmount;

					float dirtAmount = saturate(mask.g*(1+dirt.a)*2 - bloodAmount - geocol.a);
					float deDirt = 1 - dirtAmount;

					geocol = geocol*(deDirt)+dirt*dirtAmount;

					bumpMap.rg = bumpMap.rg - 0.5;// *2 - 1;
					float3 tnormal = normalize(float3(bumpMap.r+ (mask.r - maskg)*bloodAmount * 2, bumpMap.g - (mask.r - maskr)*bloodAmount * 2, 0.1));
					float3 worldNormal;
					worldNormal.x = dot(i.tspace0, tnormal);
					worldNormal.y = dot(i.tspace1, tnormal);
					worldNormal.z = dot(i.tspace2, tnormal);

					float glossyness = min(0.9, bumpMap.b*deDirt);//terrainN.b
					float ambientOcclusion = max(bumpMap.a*(deBlood+1)*0.5, dirt.a*dirtAmount);//terrainN.a

					float dotprod = max(0,dot(worldNormal,  i.viewDir.xyz));
					float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*worldNormal);

					float fernel = 1.5 - dotprod;

					float smoothness = pow(glossyness, (3 - fernel) * 2); 
					float deSmoothness = 1 - smoothness;

					float ambientBlock = 1 - ambientOcclusion;
					float3 teraBounce = _LightColor0.rgb;

					float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz)));
			
					float3 ambientSky = unity_AmbientSky.rgb * max(0, worldNormal.y - 0.5) * 2;

					float4 col = 0;

					col.rgb = geocol.rgb* (_LightColor0*diff + ambientSky);

					return col;

				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
	}
}
