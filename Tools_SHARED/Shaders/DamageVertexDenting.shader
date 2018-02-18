
Shader "Painter_Experimental/VertexDenting" {
	Properties{
	[NoScaleOffset]_DamageMask("Damage Mask", 2D) = "white" {}
	[NoScaleOffset]_MainTex("Albedo (RGB)", 2D) = "white" {}
	[NoScaleOffset]_NrmyM("Bumpy", 2D) = "white" {}
	[NoScaleOffset]_Dirt("Dirt (RGB)", 2D) = "white" {}
	[NoScaleOffset]_MainTexScratch("Scratch Diffuse (RGB)", 2D) = "white" {}
	[NoScaleOffset]_NrmyScratch("Bumpy Bloody", 2D) = "white" {}
	[NoScaleOffset]_MainTexDam("Gory Diffuse (RGB)", 2D) = "white" {}
	[NoScaleOffset]_NrmyDam("Bumpmap Bloody", 2D) = "white" {}

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


	sampler2D _MainTex;
	sampler2D _NrmyM;

	sampler2D _MainTexDam;
	sampler2D _MainTexScratch;
	sampler2D _NrmyDam;
	sampler2D _NrmyScratch;

	sampler2D _Dirt;

	sampler2D _DamageMask;


	struct v2f {
		float4 pos : POSITION;

		UNITY_FOG_COORDS(1)
		float3 viewDir : TEXCOORD2;
		float3 wpos : TEXCOORD3;
		float3 tc_Control : TEXCOORD4;
		float3 fwpos : TEXCOORD5;
		SHADOW_COORDS(6)
		//float3 normal : TEXCOORD11;
		float2 texcoord : TEXCOORD7;
		half3 tspace0 : TEXCOORD8; // tangent.x, bitangent.x, normal.x
		half3 tspace1 : TEXCOORD9; // tangent.y, bitangent.y, normal.y
		half3 tspace2 : TEXCOORD10; // tangent.z, bitangent.z, normal.z
	};

	v2f vert(appdata_full v) {
		v2f o;

		float3 worldNormal = UnityObjectToWorldNormal(v.normal);

		float4 mask = tex2Dlod(_DamageMask, float4(v.texcoord.xy,0,0));

		v.vertex.xyz -= (v.normal)*0.05*pow(mask.r,3);


		float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
		o.tc_Control.xyz = (worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.wpos = worldPos;
		o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));

		o.texcoord = v.texcoord;
		UNITY_TRANSFER_FOG(o, o.pos);
		TRANSFER_SHADOW(o);

		

		o.fwpos = foamStuff(o.wpos);

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
	float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz);

	float far = min(1, dist*0.01);
	float deFar = 1 - far;


//	float4 cont = tex2D(_mergeControl, i.tc_Control.xz);
	float4 height = tex2D(_mergeTerrainHeight, i.tc_Control.xz + _mergeTerrainScale.w);
	//float3 bump = (height.rgb - 0.5) * 2;



	float4 geocol = tex2D(_MainTex, i.texcoord.xy);
	float4 bumpMap = tex2D(_NrmyM, i.texcoord.xy);
	float4 blood = tex2D(_MainTexDam, i.texcoord.xy);
	float4 scratch = tex2D(_MainTexScratch, i.texcoord.xy*8);
	float4 dirt = tex2D(_Dirt, i.texcoord.xy*8);
	float4 mask = tex2D(_DamageMask, i.texcoord.xy);


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
	float3 tnormal = float3(bumpMap.r, bumpMap.g, 1);
	float3 worldNormal;
	worldNormal.x = dot(i.tspace0, tnormal);
	worldNormal.y = dot(i.tspace1, tnormal);
	worldNormal.z = dot(i.tspace2, tnormal);

	float glossyness = min(0.9, bumpMap.b*deDirt);//terrainN.b
	float ambientOcclusion = max(bumpMap.a*(deBlood+1)*0.5, dirt.a*dirtAmount);//terrainN.a

	float wetSection = saturate(_foamParams.w - i.fwpos.y - (1)*_foamParams.w)*(1 - glossyness);
	i.fwpos.y += 1;//cont.a;

	float dotprod = max(0,dot(worldNormal,  i.viewDir.xyz));
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*worldNormal);

	float2 foamA_W = foamAlphaWhite(i.fwpos);
	float water = max(0.5, min(i.fwpos.y + 2 - (foamA_W.x) * 2, 1)); // MODIFIED
	float under = (water - 0.5) * 2;



	glossyness = max(glossyness, wetSection*under); // MODIFIED

	float fernel = 1.5 - dotprod;

	float smoothness = (pow(glossyness, (3 - fernel) * 2));  //terrainN.b*terrainN.b;//+((1 - dotprod)*(1 - terrainN.b)));
	float deSmoothness = (1 - smoothness);

	float ambientBlock = 1 - ambientOcclusion;//max(0, 0.5 - terrainN.a);

	float shadow = saturate((SHADOW_ATTENUATION(i) * 2 - ambientBlock));


	float3 teraBounce = _LightColor0.rgb*TERABOUNCE;
	float4 terrainAmbient = tex2D(_TerrainColors, i.tc_Control.xz);
	terrainAmbient.rgb *= teraBounce;
	terrainAmbient.a *= ambientOcclusion;

	float4 terrainLight = tex2D(_TerrainColors, i.tc_Control.xz - reflected.xz*glossyness*ambientOcclusion*0.1);
	terrainLight.rgb *= teraBounce;


	float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));

	float direct = diff*shadow;

	float3 ambientSky = (unity_AmbientSky.rgb * max(0, worldNormal.y - 0.5) * 2)*terrainAmbient.a;

	float4 col;
	col.a = water; // NEW
	col.rgb = (geocol.rgb* (_LightColor0*direct + (ambientSky + terrainAmbient.rgb
		)*fernel)*deSmoothness*terrainAmbient.a + foamA_W.y*(0.5 + shadow)*(under));

	float power = smoothness * 1024;

	float up = saturate((-reflected.y - 0.5) * 2 * terrainLight.a);//

	float3 reflResult = (
		((pow(max(0.01, dot(_WorldSpaceLightPos0, -reflected)), power)* direct	*(_LightColor0)*power)) +

		terrainLight.rgb*(1 - up) +
		unity_AmbientSky.rgb *up//*terrainAmbient.a

		)* glossyness * fernel;

	col.rgb += reflResult*deDirt;

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

	return
		//geocol;
		//blood;
		col;

	}


		ENDCG
	}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}
