Shader "Painter_Experimental/VolumeBakedShadows" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map (_ATL)", Float) = 0
		[NoScaleOffset]_BumpMapC ("Combined Map", 2D) = "grey" {}
		[NoScaleOffset]_BakedShadow_VOL("Baked Shadow Volume (RGB)", 2D) = "grey" {}
		_Microdetail("Microdetail (RG-bump B:Rough A:AO)", 2D) = "white" {}
		[Toggle(VERT_SHADOW)] _vertShad("Has Vertex Shadows", Float) = 0

		[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		[NoScaleOffset]_AtlasTextures("_Textures In Row _ Atlas", float) = 1

		VOLUME_H_SLICES("Baked Shadow Slices", Vector) = (0,0,0,0)
		VOLUME_POSITION_N_SIZE("Baked Shadow Position & Size", Vector) = (0,0,0,0)
			
		l0pos("Point light 0 world scene position", Vector) = (0,0,0,0)
		l0col("Point light 0 Color", Vector) = (0,0,0,0)
		l1pos("Point light 1 world scene position", Vector) = (0,0,0,0)
		l1col("Point light 1 Color", Vector) = (0,0,0,0)
		l2pos("Point light 2 world scene position", Vector) = (0,0,0,0)
		l2col("Point light 2 Color", Vector) = (0,0,0,0)

	}
		Category{
			Tags{ "Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
		}


			//	ColorMask RGB
			//	Cull Off//Back

			SubShader{
			Pass{

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdbase
#pragma target 3.0
#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED
#pragma multi_compile  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 
#pragma multi_compile  ___ UV_ATLASED
#pragma multi_compile  ___ VERT_SHADOW

		uniform sampler2D _MainTex;
		uniform sampler2D _BumpMapC;
		uniform sampler2D _BakedShadow_VOL;
		uniform sampler2D _Microdetail;
		float4 _MainTex_ST;
		float4 _Microdetail_ST;

		float4 l0pos;
		float4 l0col;
		float4 l1pos;
		float4 l1col;
		float4 l2pos;
		float4 l2col;
		float4 _MainTex_TexelSize;
		float4 _BakedShadows_VOL_TexelSize;
		float4 VOLUME_H_SLICES;
		float4 VOLUME_POSITION_N_SIZE;

		struct v2f {
			float4 pos : SV_POSITION;
			float4 vcol : COLOR0;
			float3 worldPos : TEXCOORD0;
			float3 normal : TEXCOORD1;
			float2 texcoord : TEXCOORD2;
			float2 texcoord2 : TEXCOORD3;
			SHADOW_COORDS(4)
				float3 viewDir: TEXCOORD5;
#if defined(UV_ATLASED)
			float4 atlasedUV : TEXCOORD6;
#endif
#if !_BUMP_NONE
			float4 wTangent : TEXCOORD7;
#endif

#if VERT_SHADOW
			float4 vertShad : TEXCOORD8;
#endif

		};


		v2f vert(appdata_full v) {
			v2f o;


			o.pos = UnityObjectToClipPos(v.vertex);
			o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			o.normal.xyz = UnityObjectToWorldNormal(v.normal);

			o.vcol = v.color;
			o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

#if !_BUMP_NONE
			o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
			o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
#endif

			o.texcoord = v.texcoord.xy;//TRANSFORM_TEX(v.texcoord.xy, _MainTex); ;
			o.texcoord2 = v.texcoord1.xy;

			TRANSFER_SHADOW(o);

#if defined(UV_ATLASED)
			vert_atlasedTexture(_AtlasTextures, v.texcoord.z, _MainTex_ATL_TexelSize.x, o.atlasedUV);
#endif

#if VERT_SHADOW
			o.vertShad = v.texcoord2;
#endif

			return o;
		}



		float4 frag(v2f i) : COLOR{

			

#if UV_ATLASED

			i.texcoord2.xy = (frac(i.texcoord2.xy)*(i.atlasedUV.w) + i.atlasedUV.xy);

		float lod;

		atlasUVlod(i.texcoord, lod, _MainTex_TexelSize, i.atlasedUV);

		float4 col = tex2Dlod(_MainTex, float4(i.texcoord, 0, lod));

#if !_BUMP_NONE
		float4 bumpMap = tex2Dlod(_BumpMapC, float4(i.texcoord, 0, lod));
#endif

#else

			
			float2 tc = TRANSFORM_TEX(i.texcoord, _MainTex);
			float4 col = tex2D(_MainTex, tc);

			

#if !_BUMP_NONE
			float4 bumpMap = tex2D(_BumpMapC, tc);
#endif

#endif

			

		//	col.a *= 0.49;

#if !_BUMP_NONE
			i.texcoord.xy += 0.001 * (bumpMap.rg - 0.5);
#endif
			float4 micro = tex2D(_Microdetail, TRANSFORM_TEX(i.texcoord.xy, _Microdetail));



#if !_BUMP_NONE

			
			float3 tnormal;

#if _BUMP_REGULAR
			tnormal = UnpackNormal(bumpMap);
			bumpMap = float4(0,0,0.5,1);
#else
			bumpMap.rg = (bumpMap.rg - 0.5) * 2;
			tnormal = float3(bumpMap.r, bumpMap.g, 1);
#endif

			tnormal.rg += (micro.rg - 0.5)*0.5;

			applyTangent(i.normal, tnormal,  i.wTangent);

#else
			float4 bumpMap = float4(0,0,0.5,1);
#endif

		
			

			bumpMap.a *= micro.a;

			i.viewDir.xyz = normalize(i.viewDir.xyz);

			float dotprod = dot(i.viewDir.xyz, i.normal);
			float fernel = (1.5 - dotprod);
			float ambientBlock = (1 - bumpMap.a)*dotprod;
			
			float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal);
			ambientBlock *= 4;

			// Point Lights:
			
			float4 bake = SampleVolume(_BakedShadow_VOL, i.worldPos,  VOLUME_POSITION_N_SIZE,  VOLUME_H_SLICES, i.normal);

#if VERT_SHADOW
			bake = min(bake, i.vertShad);
#endif

			//col.a = min(col.a, 0.499);

			float4 directBake = saturate((bake - 0.9) * 10);

			const float fourPi = 4 * 3.14;

			float power = //1 - col.a*micro.b;
				//	col.a = //col.a*col.a*(1+micro.b)*0.1;
				pow(col.a, 8*micro.b)*4096;
			
			//col.a = 1 - col.a*micro.b;
			power = min(128, power);

			//return power;

			float3 scatter = 0;
			float3 glossLight = 0;
			float3 directLight = 0;

			//0 - Point Light
			
		

			float3 vec = i.worldPos.xyz - l0pos.xyz;
			float len = length(vec);
			vec /= len;

			float direct = max(0,dot(i.normal.xyz,-vec));
			direct = saturate(direct - ambientBlock * (1 - direct))*directBake.r; // Multiply by shadow

			float3 distApprox = l0col.rgb / (fourPi*(len*len));

			directLight += distApprox * direct;
			scatter += distApprox * bake.r * l0col.a;

			float3 halfDirection = normalize(i.viewDir.xyz - vec);
			float NdotH = max(0, (dot(i.normal, halfDirection)));
			float normTerm = //GGXTerm(NdotH, power);
			pow(NdotH, power)*power*0.01;

			glossLight += normTerm * l0col.rgb*direct;

			//1 - Point Light
			vec = i.worldPos.xyz - l1pos.xyz;
			len = length(vec);
			vec /= len;

			direct = max(0, dot(i.normal.xyz, -vec));
			direct = saturate(direct - ambientBlock * (1 - direct))*directBake.g; // Multiply by shadow

			distApprox = l1col.rgb / (fourPi*(len*len));

			directLight += distApprox * direct;
			scatter += distApprox * bake.g * l1col.a;


			halfDirection = normalize(i.viewDir.xyz - vec);
			NdotH = max(0.01, (dot(i.normal, halfDirection)));
			normTerm =// GGXTerm(NdotH, power);
				pow(NdotH, power)*power*0.01;

			glossLight += normTerm * l1col.rgb*direct;

			//2 - Point Light
			vec = i.worldPos.xyz - l2pos.xyz;
			len = length(vec);
			vec /= len;

			direct = max(0, dot(i.normal.xyz, -vec));
			direct = saturate(direct - ambientBlock * (1 - direct))*directBake.b; // Multiply by shadow

			distApprox = l2col.rgb / (fourPi*(len*len));

			directLight += distApprox *direct;
			scatter += distApprox * bake.b * l2col.a;


			halfDirection = normalize(i.viewDir.xyz - vec);
			//return float4(halfDirection, 0);
			NdotH = max(0.01, (dot(i.normal, halfDirection)));
			
			normTerm = //GGXTerm(NdotH, power);
				pow(NdotH, power)*power*0.01;

			//return normTerm;

			glossLight += l2col.rgb *normTerm*direct;


			// Baked Shadows End
	
			direct = max(0.01, dot(_WorldSpaceLightPos0, i.normal.xyz));
			direct = max(0.01, saturate((direct - ambientBlock * (1 - direct))*SHADOW_ATTENUATION(i))); // Multiply by shadow

			directLight += direct * _LightColor0.rgb;
			scatter = ShadeSH9(float4(i.normal, 1))*bake.a
				+ scatter * (1 - bake.a)
				;

			halfDirection = normalize(i.viewDir.xyz + _WorldSpaceLightPos0.xyz);
			NdotH = max(0.01, (dot(i.normal, halfDirection)));
			normTerm = //GGXTerm(NdotH, col.a);
				pow(NdotH, power)*power*0.01;

			glossLight += normTerm *_LightColor0.rgb*direct;

		

			col.rgb *= (directLight + (scatter) * bumpMap.a);

			col.rgb += (
				(
					glossLight
					) 
				+ ShadeSH9(float4(-reflected, 1))*bake.a
				) * (col.a)*fernel;


#if	MODIFY_BRIGHTNESS
			col.rgb *= _lightControl.a;
#endif

#if COLOR_BLEED
			float3 mix = col.gbr + col.brg;
			col.rgb += mix * mix*_lightControl.r;
#endif

		


			return col;

		}
			ENDCG

		}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
			FallBack "Diffuse"
		}

}

