#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "AutoLight.cginc"
#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_cg.cginc"

static const float MERGE_POWER = 512;
static const float TERABOUNCE = 0.2;


sampler2D _qcPp_TerrainColors;

float _Merge;

sampler2D _qcPp_WaterBump;
float4 _qcPp_foamParams;

float4 APPLY_HEIGHT_FOG(float viewDirY, float4 col, float4 fogCol) {

	viewDirY = saturate((viewDirY - _qcPp_foamParams.z) *0.01);

	return fogCol * (1 - viewDirY) + col * viewDirY;
}

float3 WORLD_POS_TO_TERRAIN_UV_3D(float3 worldPos) {
	return (worldPos.xyz - _qcPp_mergeTeraPosition.xyz) / _qcPp_mergeTerrainScale.xyz;
}

float3 SAMPLE_WATER_NORMAL(float3 viewDir, out float3 projectedWpos, inout float3 tc_Control, out float caustics, float underWater) {


	float2 v = viewDir.xz / viewDir.y;

	const float waterTyling = 0.025;

	float2 projectedxz = (_WorldSpaceCameraPos.xz - v * (_WorldSpaceCameraPos.y - _qcPp_foamParams.z));

	projectedWpos = float3(projectedxz.x, _qcPp_foamParams.z, projectedxz.y);

	float3 otcControl = WORLD_POS_TO_TERRAIN_UV_3D(projectedWpos);

	projectedxz *= waterTyling;

	float dist = length(projectedWpos - _WorldSpaceCameraPos.xyz);

	float far = min(1, dist*0.01);
	float deFar = 1 - far;

	float height = max(0, tex2D(_qcPp_mergeTerrainHeight, otcControl.xz).a);

	float2 waterUV = projectedxz; 
	float2 waterUV2 = waterUV.yx + height* waterTyling*100;//*0.01;

	waterUV -= height;

	float4 bump2B = tex2D(_qcPp_WaterBump, waterUV * 0.1 + _Time.y*0.0041);
	bump2B.rg -= 0.5;

	float4 bumpB = tex2D(_qcPp_WaterBump, waterUV2 * 0.1 - _Time.y*0.005);
	bumpB.rg -= 0.5;

	float4 bump2 = tex2Dlod(_qcPp_WaterBump, float4(waterUV + bumpB.rg*0.01
		- _Time.y*0.02, 0, bump2B.a*bumpB.a * 2));
	bump2.rg -= 0.5;

	float4 bump = tex2Dlod(_qcPp_WaterBump, float4(waterUV2 - bump2.rg*0.02
		+ bump2.rg*0.01 + _Time.y*0.032, 0, bumpB.a *bump2B.a * 2));
	bump.rg -= 0.5;

	bump.rg = (bump2.rg + bump.rg)*deFar + (bump2B.rg*bump.a + bumpB.rg*bump2.a)*0.5;

	float3 normal = normalize(float3(bump.r, 1, bump.g));

	tc_Control.xz += normal.xz *underWater*0.0005 * (1 - viewDir.y);


	float4 bump2c = tex2D(_qcPp_WaterBump, tc_Control.xz * 129 - float2(1,0.8)*_Time.y*0.02);
	//bump2c.rg = abs(bump2c.rg - 0.5);

	float4 bumpc = tex2D(_qcPp_WaterBump, tc_Control.xz * 134 - bump2c.rg*0.02 + _Time.y*0.032);
	//bumpc.rg = abs(bumpc.rg - 0.5);


	caustics = pow(1 - bumpc.b* bump2c.b, 16);

	return normal;
}

inline void APPLY_PROJECTED_WATER(float showWater, inout float3 worldNormal, float3 waterNrm, inout float3 tc_Control, float3 waterPos, 
	float viewDirY, inout float4 col, inout float smoothness, inout float ambient, inout float shadow, float caustics) {

	float deWater = 1 - showWater;

	worldNormal.xyz = waterNrm.xyz * showWater + worldNormal.xyz * deWater;

	tc_Control = WORLD_POS_TO_TERRAIN_UV_3D(waterPos) * showWater + tc_Control * deWater;

	col.rgb  *= ((caustics * _LightColor0.rgb * 45 + 1)*saturate(viewDirY)*showWater) + deWater;

	smoothness = smoothness * deWater + showWater;

	ambient = ambient * deWater + showWater;

	shadow = shadow * deWater + showWater;


}

inline void Terrain_Water_AndLight(inout float4 col, float3 tc_Control, float ambient, float smoothness, float3 worldNormal, float3 viewDir, float shadow) {

	float dotprod = max(0, dot(worldNormal, viewDir.xyz));

	float fernel = (1.5 - dotprod)*0.66;
	float3 reflected = normalize(viewDir.xyz - 2 * (dotprod)*worldNormal);// *fernel

	float deSmoothness = (1 - smoothness);

	float ambientBlock = (1 - ambient)*dotprod; // MODIFIED

	shadow = saturate(shadow * 2 - ambientBlock);

	float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	float direct = diff*shadow;

	float2 fromCenter = max(0,abs(tc_Control.xz - 0.5) - 0.5);

	float inRange = max(0, 1 - (fromCenter.x + fromCenter.y)*128);
	float outRange = 1 - inRange;

	float3 teraBounce = TERABOUNCE;

	float4 terrainAmbient = tex2Dlod(_qcPp_TerrainColors, float4(tc_Control.xz + worldNormal.xz*0.003, 0, 0)) * inRange; //outRange;

	terrainAmbient.a = tex2Dlod(_qcPp_TerrainColors, float4(tc_Control.xz, 0, 0)).a * inRange + outRange;

	terrainAmbient.rgb *= teraBounce;
	terrainAmbient.a *= ambient;
	float4 terrainLrefl = tex2Dlod(_qcPp_TerrainColors, float4(tc_Control.xz - reflected.xz*smoothness*terrainAmbient.a*0.1, 0, 6*deSmoothness))* inRange;

	terrainLrefl.rgb *= teraBounce;

	float3 ambientRefl = ShadeSH9(float4(normalize(-reflected), 1))*terrainAmbient.a;
	float3 ambientCol = ShadeSH9(float4(worldNormal, 1))*terrainAmbient.a;

	_LightColor0 *= direct;

	col.rgb = col.rgb * (_LightColor0  + (terrainAmbient.rgb + ambientCol)*fernel*terrainAmbient.a) *(0.5 + deSmoothness*0.5);
	
	float3 halfDirection = normalize(viewDir.xyz + _WorldSpaceLightPos0.xyz);

	float NdotH = max(0.01, (dot(worldNormal, halfDirection)));
	
	float power = pow(smoothness, 8) * 4096;

	float normTerm = pow(NdotH, power)*power;

	float3 reflResult = (normTerm *_LightColor0 + (terrainLrefl.rgb + ambientRefl.rgb)
		* ambient
		)* smoothness;
	
	col.rgb += reflResult;

}

inline void Terrain_4_Splats(float4 cont, float2 lowtiled, float2 tiled, float far, float deFar, inout float4 terrain, float triplanarY, inout float4 terrainN, inout float maxheight)
{

	float4 lt = float4(lowtiled, 0, getLOD(lowtiled, _qcPp_mergeSplat_4_TexelSize));
	float4 t = float4(tiled, 0, getLOD(tiled, _qcPp_mergeSplat_4_TexelSize));

	float4 splat0 = tex2Dlod(_qcPp_mergeSplat_0, lt)*far + tex2Dlod(_qcPp_mergeSplat_0, t)*deFar;
	float4 splat1 = tex2Dlod(_qcPp_mergeSplat_1, lt)*far + tex2Dlod(_qcPp_mergeSplat_1, t)*deFar;
	float4 splat2 = tex2Dlod(_qcPp_mergeSplat_2, lt)*far + tex2Dlod(_qcPp_mergeSplat_2, t)*deFar;
	float4 splat3 = tex2Dlod(_qcPp_mergeSplat_3, lt)*far + tex2Dlod(_qcPp_mergeSplat_3, t)*deFar;

	float4 splat0N = tex2Dlod(_qcPp_mergeSplatN_0, lt)*far + tex2Dlod(_qcPp_mergeSplatN_0, t)*deFar;
	float4 splat1N = tex2Dlod(_qcPp_mergeSplatN_1, lt)*far + tex2Dlod(_qcPp_mergeSplatN_1, t)*deFar;
	float4 splat2N = tex2Dlod(_qcPp_mergeSplatN_2, lt)*far + tex2Dlod(_qcPp_mergeSplatN_2, t)*deFar;
	float4 splat3N = tex2Dlod(_qcPp_mergeSplatN_3, lt)*far + tex2Dlod(_qcPp_mergeSplatN_3, t)*deFar;

	float merge = MERGE_POWER * (0.1 + deFar*0.9);

	float newHeight = cont.r * triplanarY + splat0N.b;
	float adiff = max(0, (newHeight - maxheight));
	float alpha = min(1, adiff*(1 + merge *terrain.a*splat0.a));
	float dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat0*alpha;
	terrainN = terrainN*(dAlpha)+splat0N*alpha;
	maxheight += adiff;

	newHeight = cont.g*triplanarY + splat1N.b;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + merge *terrain.a*splat1.a));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat1*alpha;
	terrainN = terrainN*(dAlpha)+splat1N*alpha;
	maxheight += adiff;

	newHeight = cont.b*triplanarY + splat2N.b;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + merge *terrain.a*splat2.a));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat2*alpha;
	terrainN = terrainN*(dAlpha)+splat2N*alpha;
	maxheight += adiff;

	newHeight = cont.a*triplanarY + splat3N.b;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + merge *terrain.a*splat3.a));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat3*alpha;
	terrainN = terrainN*(dAlpha)+splat3N*alpha;
	maxheight += adiff;

	terrainN.rg = terrainN.rg * 2 - 1;
}

inline void Terrain_Trilanear(float3 tc_Control, float3 worldPos, float dist, inout float3 worldNormal, inout float4 col, inout float4 terrainN, float4 bumpMap) {

	float far = min(1, dist*0.01);
	float deFar = 1 - far;

	float4 cont = tex2D(_qcPp_mergeControl, tc_Control.xz);
	float4 height = tex2D(_qcPp_mergeTerrainHeight, tc_Control.xz + _qcPp_mergeTerrainScale.w);
	float3 bump = (height.rgb - 0.5) * 2;

	float above = worldPos.y - _qcPp_mergeTeraPosition.y;

	float aboveTerrainBump = above - height.a*_qcPp_mergeTerrainScale.y;
	float aboveTerrainBump01 = saturate(aboveTerrainBump);
	float deAboveBump = 1 - aboveTerrainBump01;
	bump = (bump * deAboveBump + worldNormal * aboveTerrainBump01);


	float2 tiled = tc_Control.xz*_qcPp_mergeTerrainTiling.xy + _qcPp_mergeTerrainTiling.zw; // -worldNormal.xz*saturate(above - 0.5);
	float tiledY = tc_Control.y * _qcPp_mergeTeraPosition.w * 2;

	float2 lowtiled = tc_Control.xz*_qcPp_mergeTerrainTiling.xy*0.1;

	
	float4 splaty = tex2D(_qcPp_mergeSplat_4, lowtiled);//*far +//tex2D(_qcPp_mergeSplat_4, tiled)	*deFar;
	float4 splatz = tex2D(_qcPp_mergeSplat_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplat_4, float2(tiled.x, tiledY))*deFar;
	float4 splatx = tex2D(_qcPp_mergeSplat_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplat_4, float2(tiled.y, tiledY))*deFar;


	// Splat 4 is a base layer:
	float4 splatNy = tex2D(_qcPp_mergeSplatN_4, lowtiled);//*far + tex2D(_qcPp_mergeSplatN_4, tiled)*deFar;
	float4 splatNz = tex2D(_qcPp_mergeSplatN_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplatN_4, float2(tiled.x, tiledY))*deFar;
	float4 splatNx = tex2D(_qcPp_mergeSplatN_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_qcPp_mergeSplatN_4, float2(tiled.y, tiledY))*deFar;

	const float edge = MERGE_POWER;

	float4 terrain = splaty;
	terrainN = splatNy; //float4(0.5, 0.5, bumpMap.b, bumpMap.a);

	float maxheight = (1 + splatNy.b)*abs(bump.y);

	float3 newBump = float3(splatNy.x - 0.5, 0.33, splatNy.y - 0.5);

	//Triplanar X:
	float newHeight = (1.5 + splatNx.b)*abs(bump.x);
	float adiff = max(0, (newHeight - maxheight));
	float alpha = min(1, adiff*(1 + edge*terrain.a*splatx.a));
	float dAlpha = (1 - alpha);
	terrain = terrain*dAlpha + splatx*alpha;
	terrainN.ba = terrainN.ba*dAlpha + splatNx.ba*alpha;
	newBump = newBump*dAlpha + float3(0, splatNx.y - 0.5, splatNx.x - 0.5)*alpha;
	maxheight += adiff;

	//Triplanar Z:
	newHeight = (1.5 + splatNz.b)*abs(bump.z);
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + edge*terrain.a*splatz.a));
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

	Terrain_4_Splats(cont, lowtiled,  tiled,  far,  deFar,  terrain,  triplanarY,  terrainN,  maxheight);

	adiff = max(0, (tripMaxH + 0.5 - maxheight));
	alpha = min(1, adiff * 2);

	float aboveTerrain = saturate((aboveTerrainBump / _Merge - maxheight + terrainN.b - 1) * 4); // MODIFIED
	float deAboveTerrain = 1 - aboveTerrain;

	alpha *= deAboveTerrain;
	bump = tmpbump*alpha + (1 - alpha)*bump;

	worldNormal = normalize(bump
		+ float3(terrainN.r, 0, terrainN.g)*deAboveTerrain
	);

	col = col* aboveTerrain + terrain*deAboveTerrain;

	terrainN.ba = terrainN.ba * deAboveTerrain +
		aboveTerrain*bumpMap.ba;

//	col = saturate((above-0.2)*1000);

}
