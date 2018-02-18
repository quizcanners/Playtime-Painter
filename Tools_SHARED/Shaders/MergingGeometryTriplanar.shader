Shader "Terrain/MergingGeometryTriplanar" {
	Properties{
	[NoScaleOffset]_MainTex("Geometry Texture (RGB)", 2D) = "white" {}
	[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map", Float) = 0
	[NoScaleOffset]_BumpMapC("Geometry Combined Maps (RGB)", 2D) = "white" {}
	_Merge("_Merge", Range(0.01,2)) = 1
	
	}
    
		Category{
		Tags{ "RenderType" = "Opaque"
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
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
//#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "VertexDataProcessInclude.cginc"

#pragma multi_compile_fwdbase //nolightmap nodirlightmap nodynlightmap novertexlight
#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED
#pragma multi_compile  ___ WATER_FOAM
#pragma multi_compile  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 

	
	sampler2D _MainTex;
	sampler2D _BumpMapC;
	




	struct v2f {
		float4 pos : POSITION;

		UNITY_FOG_COORDS(1)
		float3 viewDir : TEXCOORD2;
		float3 wpos : TEXCOORD3;
		float3 tc_Control : TEXCOORD4;
		float3 fwpos : TEXCOORD5;
		SHADOW_COORDS(6)
		float2 texcoord : TEXCOORD7;
#if _BUMP_NONE
		float3 normal : TEXCOORD8;
#else
		float3 tspace0 : TEXCOORD8; 
		float3 tspace1 : TEXCOORD9; 
		float3 tspace2 : TEXCOORD10; 
#endif
	};

	v2f vert(appdata_full v) {
		v2f o;

		float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
		o.tc_Control.xyz = (worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.wpos = worldPos;
		o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));

		o.texcoord = v.texcoord;
		UNITY_TRANSFER_FOG(o, o.pos);
		TRANSFER_SHADOW(o);

		float3 worldNormal = UnityObjectToWorldNormal(v.normal);

		o.fwpos = foamStuff(o.wpos);      

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

	float4 col = tex2D(_MainTex, i.texcoord.xy);
	
#if _BUMP_NONE
	float3 worldNormal = i.normal;
	float4 bumpMap = float4(0, 0, 0.5, 1);
#else

	float4 bumpMap = tex2D(_BumpMapC, i.texcoord.xy);
	float3 tnormal;
#if _BUMP_REGULAR
	tnormal = UnpackNormal(bumpMap);
	bumpMap = float4(0, 0, 0.5, 1);
#else
	bumpMap.rg = (bumpMap.rg - 0.5) * 2;
	tnormal = float3(bumpMap.r, bumpMap.g, 1);
#endif
	//applyTangent(worldNormal, tnormal, i.wTangent);
	

	//bumpMap.rg -= 0.5;// bumpMap.rg - 0.5;// *2 - 1;
	//float3 tnormal = float3(bumpMap.r, bumpMap.g, 1);
	float3 worldNormal;
	worldNormal.x = dot(i.tspace0, tnormal);
	worldNormal.y = dot(i.tspace1, tnormal);
	worldNormal.z = dot(i.tspace2, tnormal);
#endif

	// Terrain Start
	float4 terrainN = 0;

	Terrain_Trilanear(i.tc_Control, i.wpos, dist, worldNormal, col, terrainN, bumpMap);

	float shadow = SHADOW_ATTENUATION(i);
#if WATER_FOAM
	float2 wet = WetSection(terrainN, col.a, i.fwpos, shadow);
#endif

	Terrain_Light(i.tc_Control, terrainN, worldNormal, i.viewDir.xyz,
		col, shadow);

#if WATER_FOAM
	col.rgb += wet.x;
	col.a = wet.y;
	col.rgb *= 1 - saturate((_foamParams.z - i.wpos.y)*0.1);  // NEW

#endif

	float4 fogged = col;
	UNITY_APPLY_FOG(i.fogCoord, fogged);
	float fogging = (32 - max(0,i.wpos.y - _foamParams.z)) / 32;

	fogging = min(1,pow(max(0,fogging),2));
	col.rgb = fogged.rgb * fogging + col.rgb *(1 - fogging);

#if	MODIFY_BRIGHTNESS
	col.rgb *= _lightControl.a;
#endif

#if COLOR_BLEED
	float3 mix = col.gbr + col.brg;
	col.rgb += mix*mix*_lightControl.r;
#endif

	//col.rgb = bump.rgb;
	//col.rgb = worldNormal;
	//col.rg = abs(reflected.xz);
	//col.b = 0;
	//terrainLrefl.rgb *= cont.rgb;
	return
		//ambientPower;
		//micro;
		//power;//
		//terrainLrefl;//*(1 - smoothness);
		//smoothness;
		//cont;
		//power;
		//splat0N;
		//terrainAmbient;
		//fernel;
		//diff;
		//aboveTerrainBump;
		//terrainLrefl;
		col;
	//dotprod;
	//terrainAmbient;
	}


		ENDCG
	}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}
