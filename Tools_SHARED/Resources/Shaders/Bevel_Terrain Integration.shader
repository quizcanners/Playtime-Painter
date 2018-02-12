Shader "Bevel/Bevel_Terrain Integration" {
	Properties{
		[NoScaleOffset]_MainTex("Base texture Atlas", 2D) = "white" {}
	[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map", Float) = 0
		[NoScaleOffset]_BumpMapC("Combined Maps Atlas (RGB)", 2D) = "white" {}
	[Toggle(UV_PROJECTED)] _PROJECTED("Projected UV", Float) = 0
		_Merge("_Merge", Range(0.01,25)) = 1
		[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		[NoScaleOffset]_AtlasTextures("_Textures In Row _ Atlas", float) = 1

		[Toggle(EDGE_WIDTH_FROM_COL_A)] _EDGE_WIDTH("Color A as Edge Width", Float) = 0
		[Toggle(CLIP_EDGES)] _CLIP("Clip Edges", Float) = 0

		[Toggle(UV_PIXELATED)] _PIXELATED("Smooth Pixelated", Float) = 0


	}

		SubShader{

		Tags{
		"Queue" = "Geometry"
		"IgnoreProjector" = "True"
		"RenderType" = "Opaque"
		"LightMode" = "ForwardBase"
		"DisableBatching" = "True"
		"Solution" = "Bevel"
	}

		ColorMask RGBA

		Pass{


		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "VertexDataProcessInclude.cginc"

#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
#pragma multi_compile_fog
#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED
#pragma multi_compile  ___ UV_ATLASED
#pragma multi_compile  ___ UV_PROJECTED
#pragma multi_compile  ___ UV_PIXELATED
#pragma multi_compile  ___ EDGE_WIDTH_FROM_COL_A
#pragma multi_compile  ___ CLIP_EDGES
#pragma multi_compile  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 


	sampler2D _MainTex;
	sampler2D _BumpMapC;
	float4 _MainTex_TexelSize;
	float _AtlasTextures;

	struct v2f {
		float4 pos : SV_POSITION;
		float4 vcol : COLOR0;
		float3 worldPos : TEXCOORD0;
		float3 normal : TEXCOORD1;
		float2 texcoord : TEXCOORD2;
		float4 edge : TEXCOORD3;
		float3 snormal: TEXCOORD4;
		SHADOW_COORDS(5)
		float3 viewDir: TEXCOORD6;
		float3 edgeNorm0 : TEXCOORD7;
		float3 edgeNorm1 : TEXCOORD8;
		float3 edgeNorm2 : TEXCOORD9;
#if UV_ATLASED
		float4 atlasedUV : TEXCOORD10;
#endif

#if !_BUMP_NONE
#if UV_PROJECTED
		float4 bC : TEXCOORD11;
#else
		float4 wTangent : TEXCOORD11;
#endif
#endif
		UNITY_FOG_COORDS(12)
		float3 tc_Control : TEXCOORD13;
		float3 fwpos : TEXCOORD14;
	};

	v2f vert(appdata_full v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		UNITY_TRANSFER_FOG(o, o.pos);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.fwpos = foamStuff(o.worldPos);
		o.tc_Control.xyz = (o.worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);

		o.vcol = v.color;
		o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w);
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

		float3 deEdge = 1 - o.edge.xyz;

		o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
		o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
		o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

		o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

#if UV_PROJECTED
		normalAndPositionToUV(o.snormal.xyz, o.worldPos,
#if !_BUMP_NONE
			o.bC,
#endif
			o.texcoord.xy);
#else

#if !_BUMP_NONE
		o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
		o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
#endif

		o.texcoord = v.texcoord.xy;

#endif

		TRANSFER_SHADOW(o);

#if UV_ATLASED
		atlasedTexture(_AtlasTextures, v.texcoord.z, _MainTex_TexelSize.x, o.atlasedUV);
#endif

		return o;
	}



	float4 frag(v2f i) : SV_Target{

		i.viewDir.xyz = normalize(i.viewDir.xyz);

	float dist = length(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);

	float far = min(1, dist*0.01);
	float deFar = 1 - far;

		float mip = 0;

#if UV_ATLASED
	
#if	!UV_PIXELATED
	mip = (log2(dist));
#endif
	float seam = i.atlasedUV.z*pow(2, mip);
	float2 fractal = (frac(i.texcoord.xy)*(i.atlasedUV.w - seam) + seam*0.5);
	i.texcoord = fractal + i.atlasedUV.xy;
#endif



#if	UV_PIXELATED
	float2 perfTex = (floor(i.texcoord*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;
	float2 off = (i.texcoord - perfTex);
	off = off *saturate((abs(off) * _MainTex_TexelSize.z) * 40 - 19);
	i.texcoord = perfTex + off;
#endif

#if UV_ATLASED || UV_PIXELATED
	float4 col = tex2Dlod(_MainTex, float4(i.texcoord,0,mip));
#else
	float4 col = tex2D(_MainTex, i.texcoord);
#endif

	float weight;
	float3 worldNormal = DetectSmoothEdge(
#if EDGE_WIDTH_FROM_COL_A
		col.a,
#endif
		i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight);

	float deWeight = 1 - weight;

#if CLIP_EDGES
	clip(dot(i.viewDir.xyz, worldNormal));
#endif

	col = col*deWeight + i.vcol*weight;

#if !_BUMP_NONE


#if UV_ATLASED || UV_PIXELATED
	float4 bumpMap = tex2Dlod(_BumpMapC, float4(i.texcoord, 0, mip));
#else
	float4 bumpMap = tex2D(_BumpMapC, i.texcoord);
#endif

	float3 tnormal;

#if _BUMP_REGULAR
	tnormal = UnpackNormal(bumpMap);
	bumpMap = float4(0,0,0.5,1);
#else
	bumpMap.rg = (bumpMap.rg - 0.5) * 2;
	tnormal = float3(bumpMap.r, bumpMap.g, 1);
#endif


	float3 preNorm = worldNormal;

#if UV_PROJECTED
	applyTangentNonNormalized(i.bC, worldNormal, bumpMap.rg);
	worldNormal = normalize(worldNormal);
#else
	applyTangent(worldNormal, tnormal,  i.wTangent);
#endif

	worldNormal = worldNormal*deWeight + preNorm*weight;

#else
	float4 bumpMap = float4(0,0,0.5,1);
#endif

	bumpMap.b = bumpMap.b*deWeight + weight*i.vcol.a;
	bumpMap.a = bumpMap.a*deWeight + weight*0.7;

	// Terrain Start
	float4 cont = tex2D(_mergeControl, i.tc_Control.xz);
	float4 height = tex2D(_mergeTerrainHeight, i.tc_Control.xz + _mergeTerrainScale.w);
	float3 bump = (height.rgb - 0.5) * 2;

	float aboveTerrainBump = ((((i.worldPos.y - _mergeTeraPosition.y) - height.a*_mergeTerrainScale.y)));
	float aboveTerrainBump01 = saturate(aboveTerrainBump);
	float deAboveBump = 1 - aboveTerrainBump01;
	bump = (bump * deAboveBump + worldNormal * aboveTerrainBump01);


	float2 tiled = i.tc_Control.xz*_mergeTerrainTiling.xy + _mergeTerrainTiling.zw;
	float tiledY = i.tc_Control.y * _mergeTeraPosition.w * 2;

	float2 lowtiled = i.tc_Control.xz*_mergeTerrainTiling.xy*0.1;

	float4 splat0 = tex2D(_mergeSplat_0, lowtiled)*far + tex2D(_mergeSplat_0, tiled)*deFar;
	float4 splat1 = tex2D(_mergeSplat_1, lowtiled)*far + tex2D(_mergeSplat_1, tiled)*deFar;
	float4 splat2 = tex2D(_mergeSplat_2, lowtiled)*far + tex2D(_mergeSplat_2, tiled)*deFar;
	float4 splat3 = tex2D(_mergeSplat_3, lowtiled)*far + tex2D(_mergeSplat_3, tiled)*deFar;

	float4 splaty = tex2D(_mergeSplat_4, lowtiled);//*far +//tex2D(_mergeSplat_4, tiled)	*deFar;
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

	const float edge = MERGE_POWER;

	float4 terrain = splaty;
	float4 terrainN = splatNy; //float4(0.5, 0.5, bumpMap.b, bumpMap.a);

	float maxheight = (1 + splaty.a)*abs(bump.y);

	float3 newBump = float3(splatNy.x - 0.5, 0.33, splatNy.y - 0.5);

	//Triplanar X:
	float newHeight = (1.5 + splatx.a)*abs(bump.x);
	float adiff = max(0, (newHeight - maxheight));
	float alpha = min(1, adiff*(1 + edge*terrainN.b*splatNx.b));
	float dAlpha = (1 - alpha);
	terrain = terrain*dAlpha + splatx*alpha;
	terrainN.ba = terrainN.ba*dAlpha + splatNx.ba*alpha;
	newBump = newBump*dAlpha + float3(0, splatNx.y - 0.5, splatNx.x - 0.5)*alpha;
	maxheight += adiff;

	//Triplanar Z:
	newHeight = (1.5 + splatz.a)*abs(bump.z);
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + edge*terrainN.b*splatNz.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splatz*alpha;
	terrainN.ba = terrainN.ba*dAlpha + splatNz.ba*alpha;
	newBump = newBump*dAlpha + float3(splatNz.x - 0.5, splatNz.y - 0.5, 0)*alpha;
	maxheight += adiff;

	terrainN.rg = 0.5;

	float tripMaxH = maxheight;
	float3 tmpbump = normalize(bump + newBump * 2 * deAboveBump);

	terrain = terrain*deAboveBump + col*aboveTerrainBump01;

	float triplanarY = max(0, tmpbump.y) * 2; // Recalculate it based on previously sampled bump

	newHeight = cont.r * triplanarY + splat0.a;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + edge*terrainN.b*splat0N.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat0*alpha;
	terrainN = terrainN*(dAlpha)+splat0N*alpha;
	maxheight += adiff;


	newHeight = cont.g*triplanarY + splat1.a;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + edge*terrainN.b*splat1N.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat1*alpha;
	terrainN = terrainN*(dAlpha)+splat1N*alpha;
	maxheight += adiff;


	newHeight = cont.b*triplanarY + splat2.a;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + edge*terrainN.b*splat2N.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat2*alpha;
	terrainN = terrainN*(dAlpha)+splat2N*alpha;
	maxheight += adiff;

	newHeight = cont.a*triplanarY + splat3.a;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + edge*terrainN.b*splat3N.b));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat3*alpha;
	terrainN = terrainN*(dAlpha)+splat3N*alpha;
	maxheight += adiff;


	terrainN.rg = terrainN.rg * 2 - 1;

	adiff = max(0, (tripMaxH + 0.5 - maxheight));
	alpha = min(1, adiff * 2);

	float aboveTerrain = saturate((aboveTerrainBump / _Merge - maxheight + col.a - 1) * 4); // MODIFIED
	float deAboveTerrain = 1 - aboveTerrain;

	alpha *= deAboveTerrain;
	bump = tmpbump*alpha + (1 - alpha)*bump;

	cont = col* aboveTerrain + terrain*deAboveTerrain;

	float wetSection = saturate(_foamParams.w - i.fwpos.y - (cont.a)*_foamParams.w)*(1 - terrainN.b);
	i.fwpos.y += cont.a;

	worldNormal = normalize(bump
		+ float3(terrainN.r, 0, terrainN.g)*deAboveTerrain
	);

	terrainN.ba = terrainN.ba * deAboveTerrain +
		aboveTerrain*bumpMap.ba;

	float dotprod = max(0, dot(worldNormal, i.viewDir.xyz));
	float fernel = 1.5 - dotprod;
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*worldNormal);// *fernel

	float2 foamA_W = foamAlphaWhite(i.fwpos);
	float water = max(0.5, min(i.fwpos.y + 2 - (foamA_W.x) * 2, 1));
	float under = (water - 0.5) * 2;

	terrainN.b = max(terrainN.b, wetSection*under);

	float smoothness = (pow(terrainN.b, (3 - fernel) * 2));
	float deSmoothness = (1 - smoothness);

	float ambientBlock = (1 - terrainN.a)*dotprod; // MODIFIED

	float shadow = saturate((SHADOW_ATTENUATION(i) * 2 - ambientBlock));

	float3 teraBounce = _LightColor0.rgb*TERABOUNCE;
	float4 terrainAmbient = tex2D(_TerrainColors, i.tc_Control.xz);
	terrainAmbient.rgb *= teraBounce;
	terrainAmbient.a *= terrainN.a;

	float4 terrainLrefl = tex2D(_TerrainColors, i.tc_Control.xz
		- reflected.xz*terrainN.b*terrainAmbient.a*0.1
	);
	terrainLrefl.rgb *= teraBounce;

	bumpMap = terrainN;

	// Terrain End

	float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	float direct = diff*shadow;

	float3 ambientRefl = ShadeSH9(float4(reflected, 1))*terrainAmbient.a;
	float3 ambientCol = ShadeSH9(float4(worldNormal, 1))*terrainAmbient.a;

	col.a = water;

	col.rgb = (cont.rgb* (_LightColor0*direct + (terrainAmbient.rgb+ ambientCol
		)*fernel)*deSmoothness*terrainAmbient.a + foamA_W.y*(0.5 + shadow)*(under));

	float power = smoothness * 1024;

	float3 reflResult = (
		((pow(max(0.01, dot(_WorldSpaceLightPos0, -reflected)), power)* direct	*(_LightColor0)*power)) +

		terrainLrefl.rgb +
		ambientRefl.rgb

		)* terrainN.b * fernel;

	col.rgb += reflResult * under;

	col.rgb *= 1 - saturate((_foamParams.z - i.worldPos.y)*0.1);  // NEW


	float4 fogged = col;
	UNITY_APPLY_FOG(i.fogCoord, fogged);
	float fogging = (32 - max(0, i.worldPos.y - _foamParams.z)) / 32;

	fogging = min(1, pow(max(0, fogging), 2));
	col.rgb = fogged.rgb * fogging + col.rgb *(1 - fogging);

#if	MODIFY_BRIGHTNESS
	col.rgb *= _lightControl.a;
#endif

#if COLOR_BLEED
	float3 mix = col.gbr + col.brg;
	col.rgb += mix*mix*_lightControl.r;
#endif


	return col;

	}
		ENDCG

	}



		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

	}

		//CustomEditor "BevelMaterialInspector"

}