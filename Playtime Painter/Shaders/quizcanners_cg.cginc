#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "AutoLight.cginc"

static const float MERGE_POWER = 512;
static const float TERABOUNCE = 0.2;

float4 g_VOLUME_H_SLICES;
float4 g_VOLUME_POSITION_N_SIZE;

float4 g_l0pos;
float4 g_l0col;
float4 g_l1pos;
float4 g_l1col;
float4 g_l2pos;
float4 g_l2col;

sampler2D _Global_Noise_Lookup;

float4 pp_COLOR_BLEED;
sampler2D _mergeTerrainHeight;
float4 _mergeTerrainHeight_TexelSize;
float4 _wrldOffset;

sampler2D _TerrainColors;
sampler2D _mergeControl;

float4 _Control_ST;
float4 _mergeTerrainTiling;
float4 _mergeTeraPosition;
float4 _mergeTerrainScale;
float _Merge;

sampler2D _mergeSplat_0;
sampler2D _mergeSplat_1;
sampler2D _mergeSplat_2;
sampler2D _mergeSplat_3;
sampler2D _mergeSplat_4;

float4 _mergeSplat_4_TexelSize;

sampler2D _mergeSplatN_0;
sampler2D _mergeSplatN_1;
sampler2D _mergeSplatN_2;
sampler2D _mergeSplatN_3;
sampler2D _mergeSplatN_4;

sampler2D _pp_WaterBump;
float4 _foamParams;



uniform sampler2D g_BakedRays_VOL;
uniform sampler2D _pp_RayProjectorDepthes;
float4 g_BakedRays_VOL_TexelSize;

float4x4 rt0_ProjectorMatrix;
float4 rt0_ProjectorPosition;
float4 rt0_ProjectorClipPrecompute;
float4 rt0_ProjectorConfiguration;

float4x4 rt1_ProjectorMatrix;
float4 rt1_ProjectorPosition;
float4 rt1_ProjectorClipPrecompute;
float4 rt1_ProjectorConfiguration;

float4x4 rt2_ProjectorMatrix;
float4 rt2_ProjectorPosition;
float4 rt2_ProjectorClipPrecompute;
float4 rt2_ProjectorConfiguration;


float4 APPLY_HEIGHT_FOG(float viewDirY, float4 col, float4 fogCol) {

	viewDirY = saturate((viewDirY - _foamParams.z) *0.01);

	return fogCol * (1 - viewDirY)  + col * viewDirY;

}

float3 WORLD_POS_TO_TERRAIN_UV_3D(float3 worldPos) {
	return (worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;
}

float3 SAMPLE_WATER_NORMAL(float3 viewDir, out float3 projectedWpos) {

	float2 v = viewDir.xz / viewDir.y;

	const float waterTyling = 0.025;

	float2 projectedxz = (_WorldSpaceCameraPos.xz - v * (_WorldSpaceCameraPos.y - _foamParams.z));

	projectedWpos = float3(projectedxz.x, _foamParams.z, projectedxz.y);

	float3 otcControl = WORLD_POS_TO_TERRAIN_UV_3D(projectedWpos);

	projectedxz *= waterTyling;

	float dist = length(projectedWpos - _WorldSpaceCameraPos.xyz);

	float far = min(1, dist*0.01);
	float deFar = 1 - far;

	float height = max(0, tex2D(_mergeTerrainHeight, otcControl.xz).a);

	float2 waterUV = projectedxz; 
	float2 waterUV2 = waterUV.yx + height* waterTyling*100;//*0.01;

	waterUV -= height;

	float4 bump2B = tex2D(_pp_WaterBump, waterUV * 0.1 + _Time.y*0.0041);
	bump2B.rg -= 0.5;

	float4 bumpB = tex2D(_pp_WaterBump, waterUV2 * 0.1 - _Time.y*0.005);
	bumpB.rg -= 0.5;

	float4 bump2 = tex2Dlod(_pp_WaterBump, float4(waterUV + bumpB.rg*0.01
		- _Time.y*0.02, 0, bump2B.a*bumpB.a * 2));
	bump2.rg -= 0.5;

	float4 bump = tex2Dlod(_pp_WaterBump, float4(waterUV2 - bump2.rg*0.02
		+ bump2.rg*0.01 + _Time.y*0.032, 0, bumpB.a *bump2B.a * 2));
	bump.rg -= 0.5;

	bump.rg = (bump2.rg + bump.rg)*deFar + (bump2B.rg*bump.a + bumpB.rg*bump2.a)*0.5;

	return normalize(float3(bump.r, 1, bump.g));
}

float4 ProjectorUvDepthAlpha(float4 shadowCoords, float3 worldPos, float3 lightPos, float4 cfg, float4 precompute) {

	float camAspectRatio = cfg.x;
	float camFOVDegrees = cfg.y;
	float deFar = cfg.w;
	float near = cfg.z;

	shadowCoords.xy /= shadowCoords.w;
	
	float3 diff = worldPos - lightPos;
		
	float dist = length(diff);

	float alpha = max(0, sign(shadowCoords.w) - dot(shadowCoords.xy, shadowCoords.xy) - max(0, near - dist)*8);

	float viewPos = length(float3(shadowCoords.xy * camFOVDegrees, 1))*camAspectRatio;

	float true01Range = dist * deFar;

	float predictedDepth = 1 - (((viewPos / true01Range) - precompute.y) * precompute.z);

	return float4((shadowCoords.xy + 1) * 0.5, predictedDepth+0.01 , alpha);

}


float3 GetRayTracedShadows(float3 posNrm, float3 norm, float4 shadowCoords0, float4 shadowCoords1, float4 shadowCoords2 ) {

	float near = rt0_ProjectorConfiguration.z;

	float3 shads;

	float distance = rt0_ProjectorClipPrecompute.w;

	float4 shUv0 = ProjectorUvDepthAlpha(
		shadowCoords0, posNrm //+ norm * 0.1 * distance
		,
		rt0_ProjectorPosition.rgb,
		rt0_ProjectorConfiguration,
		rt0_ProjectorClipPrecompute);
	
	const float sharpness = 1024;

	float depth = tex2Dlod(_pp_RayProjectorDepthes, float4(shUv0.xy, 0, 0)).r;

	shads.r = (1 - saturate((depth - shUv0.z) * sharpness / near)) * shUv0.w;

	near = rt1_ProjectorConfiguration.z;

	distance = rt1_ProjectorClipPrecompute.w;

	float4 shUv1 = ProjectorUvDepthAlpha(
		shadowCoords1, posNrm //+ norm * 0.1 * distance
		,
		rt1_ProjectorPosition.rgb,
		rt1_ProjectorConfiguration,
		rt1_ProjectorClipPrecompute);

	depth = tex2Dlod(_pp_RayProjectorDepthes, float4(shUv1.xy, 0, 0)).g;

	shads.g = (1 - saturate((depth - shUv1.z) * sharpness / near)) * shUv1.w;

	near = rt2_ProjectorConfiguration.z;

	distance = rt2_ProjectorClipPrecompute.w;

	float4 shUv2 = ProjectorUvDepthAlpha(
		shadowCoords2, posNrm //+ norm * 0.1 * distance
		,
		rt2_ProjectorPosition.rgb,
		rt2_ProjectorConfiguration,
		rt2_ProjectorClipPrecompute);

	depth = tex2Dlod(_pp_RayProjectorDepthes, float4(shUv2.xy, 0, 0)).b;

	shads.b = (1 - saturate((depth - shUv2.z) * sharpness / (near * (1- depth)))) * shUv2.w;

	return shads;

}

inline void vert_atlasedTexture(float _AtlasTextures, float atlasNumber, out float4 atlasedUV) {
	float atY = floor(atlasNumber / _AtlasTextures);
	float atX = atlasNumber - atY * _AtlasTextures;
	atlasedUV.xy = float2(atX, atY) / _AtlasTextures;				
	atlasedUV.z = _AtlasTextures;										
	atlasedUV.w = 1 / _AtlasTextures;
}

// Old depricated
inline void vert_atlasedTexture(float _AtlasTextures, float atlasNumber, float _TexelSizeX, out float4 atlasedUV) {
	float atY = floor(atlasNumber / _AtlasTextures);
	float atX = atlasNumber - atY*_AtlasTextures;
	atlasedUV.xy = float2(atX, atY) / _AtlasTextures;				//+edge;
	atlasedUV.z = _TexelSizeX;										//(1) / _AtlasTextures - edge * 2;
	atlasedUV.w = 1 / _AtlasTextures;
}

inline float getLOD(float2 uv, float4 _TexelSize) {

	float2 px = _TexelSize.z * ddx(uv);
	float2 py = _TexelSize.w * ddy(uv);

	return max(0, 0.5 * log2(max(dot(px, px), dot(py, py))));
}

inline float getLOD(float2 uv, float4 _TexelSize, float mod) {
	//_TexelSize.zw *= mod;
	//float2 px = _TexelSize.z * ddx(uv);
	//float2 py = _TexelSize.w * ddy(uv);

	return getLOD(uv,  _TexelSize * mod); //max(0, 0.5 * log2(max(dot(px, px), dot(py, py))));
}

inline float atlasUVlod(inout float2 uv, out float lod, float4 _TexelSize,  float4 atlasedUV) {

	_TexelSize.zw *= 0.5 * atlasedUV.w;
	float2 px = _TexelSize.z * ddx(uv);
	float2 py = _TexelSize.w * ddy(uv);
	
	lod = max(0, 0.5 * log2(max(dot(px, px), dot(py, py))));
	
	float seam = (_TexelSize.x)*pow(2, lod);

	uv = frac(uv)*(atlasedUV.w - seam) + atlasedUV.xy + seam * 0.5;

	return seam;
}

inline void atlasUV(inout float2 uv, float seam, float4 atlasedUV) {
	uv = frac(uv)*(atlasedUV.w - seam) + atlasedUV.xy + seam * 0.5;
}


// Old depricated
inline void frag_atlasedTexture(float4 atlasedUV, float mip, inout float2 uv) {
	float seam = (atlasedUV.z)*pow(2, mip);
	float2 fractal = (frac(uv)*(atlasedUV.w - seam) + seam * 0.5);
	uv = fractal + atlasedUV.xy;
}

inline void applyTangent (inout float3 normal, float3 tnormal, float4 wTangent){
	float3 wBitangent = cross(normal, wTangent.xyz) * wTangent.w;

	float3 tspace0 = float3(wTangent.x, wBitangent.x, normal.x);
	float3 tspace1 = float3(wTangent.y, wBitangent.y, normal.y);
	float3 tspace2 = float3(wTangent.z, wBitangent.z, normal.z);																												

	normal.x = dot(tspace0, tnormal);
	normal.y = dot(tspace1, tnormal);
	normal.z = dot(tspace2, tnormal);
}

inline void normalAndPositionToUV (float3 worldNormal, float3 scenepos, out float4 tang, out float2 uv){

	scenepos += _wrldOffset.xyz;

	worldNormal = abs(worldNormal);
	float znorm = saturate((worldNormal.x-worldNormal.z)*55555);
	float xnorm = saturate(((worldNormal.z+worldNormal.y) - worldNormal.x)*55555);
	float ynorm = saturate((worldNormal.y-0.8)*55555);
					
	float x = (scenepos.x)*(xnorm)+(scenepos.z)*(1-xnorm);
	float y = (scenepos.y)*(1-ynorm)+(scenepos.z)*(ynorm);

	uv.x = x;
	uv.y = y;

	float dey = 1-ynorm;

	tang.w = xnorm*ynorm;
	tang.x = tang.w + (1-znorm)*dey;
	tang.y = dey; 
	tang.z = znorm*dey; 

}

inline void normalAndPositionToUV(float3 worldNormal, float3 scenepos, out float2 uv) {

	scenepos += _wrldOffset.xyz;

	worldNormal = abs(worldNormal);
	float znorm = saturate((worldNormal.x - worldNormal.z) * 55555);
	float xnorm = saturate(((worldNormal.z + worldNormal.y) - worldNormal.x) * 55555);
	float ynorm = saturate((worldNormal.y - 0.8) * 55555);

	float x = (scenepos.x)*(xnorm)+(scenepos.z)*(1 - xnorm);
	float y = (scenepos.y)*(1 - ynorm) + (scenepos.z)*(ynorm);

	uv.x = x;
	uv.y = y;

}

inline void applyTangentNonNormalized (float4 tang, inout float3 normal, float2 bump){

	normal.xyz+=float3(bump.x*tang.x, bump.y*tang.y, bump.x*tang.z + bump.y*tang.w); 


}

inline void rotate ( inout float2 uv, float angle){

		float sinX = sin ( angle );
		float cosX = cos ( angle );
		float sinY = sin ( angle );
		float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);

		uv =  mul ( uv, rotationMatrix );

}

inline void smoothedPixelsSampling (inout float2 texcoord, float4 _TexelSize, out float mip) {


		float2 px = _TexelSize.z * ddx(texcoord);
		float2 py = _TexelSize.w * ddy(texcoord);



		mip = max(0, 0.5 * log2(max(dot(px, px), dot(py, py))));
		float mipped = saturate(1 - mip);

		float2 perfTex = (floor(texcoord.xy*_TexelSize.z) + 0.5) * _TexelSize.x;
		
		float2 off = (texcoord.xy - perfTex);

		float n = 30;//max(4,30 );

		float2 offset = saturate((abs(off) * _TexelSize.z)*(n*2+2) - n);

		off = off * offset;

		texcoord.xy =  (perfTex + off)*mipped + texcoord.xy*(1-mipped);

}

inline void smoothedPixelsSampling(inout float2 texcoord, float4 texelsSize) {

	float2 perfTex = (floor(texcoord*texelsSize.z) + 0.5) * texelsSize.x;
	float2 off = (texcoord - perfTex);
	off = off *saturate((abs(off) * texelsSize.z) * 40 - 19);
	texcoord = perfTex + off;

}

inline float2 DetectEdge(float4 edge){
	edge = max(0, edge - 0.965) * 28.5715;
	float border = max(max(edge.r, edge.g), edge.b);
	return float2(border, min(1,edge.a)*border);
}

inline float3 HUEtoColor(float hue) {

	float val = frac(hue+0.082) * 6;

	float3 col;

	col.r = saturate(2 - abs(val - 2));

	val = fmod((val + 2), 6);

	col.g = saturate(2 - abs(val - 2));

	val = fmod((val + 2), 6);

	col.b = saturate(2 - abs(val - 2));

	col.rgb = pow(col.rgb, 2.2);

	return col;
}

inline float3 DetectSmoothEdge(float4 edge, float3 junkNorm, float3 sharpNorm, float3 edge0, float3 edge1, float3 edge2, out float weight) {

	edge = max(0, edge - 0.965) * 28.5715;

	float border = max(max(edge.r, edge.g), edge.b);

	float3 edgeN = edge0*edge.r + edge1*edge.g + edge2*edge.b;

	float junk = min(1, (edge.g*edge.b + edge.r*edge.b + edge.r*edge.g)*2)* border;

	weight = (edge.w)*border;

	return normalize((sharpNorm*(1 - border) + border*edgeN)*(1 - junk) + junk*(junkNorm));

}

inline float3 DetectSmoothEdge(float thickness ,float4 edge, float3 junkNorm, float3 sharpNorm, float3 edge0, float3 edge1, float3 edge2, out float weight) {

	thickness = thickness*thickness*0.25;

	edge = saturate((edge - 1 + thickness)/thickness);

	float border = max(max(edge.r, edge.g), edge.b);

	float3 edgeN = edge0*edge.r + edge1*edge.g + edge2*edge.b;

	float junk = min(1, (edge.g*edge.b + edge.r*edge.b + edge.r*edge.g) * 2)* border;

	weight = (edge.w)*border;

	return normalize((sharpNorm*(1 - border) + border*edgeN)*(1 - junk) + junk*(junkNorm));

}

inline float3 reflectedVector (float3 normal, float3 viewDir){

	float dotprod = dot(normal.xyz, viewDir.xyz);
	return  normalize(viewDir.xyz - 2*(dotprod)*normal.xyz); 

}

inline void leakColors (inout float4 col){

	float3 flow = (col.gbr + col.brg);
	flow *= flow;
	col.rgb += flow*0.02;

}

inline void BleedAndBrightness(inout float4 col, float mod) {

	col.rgb *= pp_COLOR_BLEED.a;

	float3 mix = min(col.gbr + col.brg, 128)*mod;
	col.rgb += mix * mix*pp_COLOR_BLEED.r;

}

inline void Simple_Light(float4 terrainN,float3 worldNormal, float3 viewDir, inout float4 col, float shadow, float reflectivness) {

	float dotprod = max(0, dot(worldNormal, viewDir.xyz));
	float fernel = 1.5 - dotprod;
	float3 reflected = normalize(viewDir.xyz - 2 * (dotprod)*worldNormal);

	float smoothness = col.a;
	float deSmoothness = (1 - smoothness);

	float ambientBlock = (1 - terrainN.a)*dotprod; // MODIFIED

	shadow = saturate((shadow * 2 - ambientBlock));

	float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	float direct = diff*shadow;


	float3 ambientRefl = ShadeSH9(float4(reflected, 1));
	float3 ambientCol = ShadeSH9(float4(worldNormal, 1));

	_LightColor0 *= direct;

	col.rgb = col.rgb* (_LightColor0 +
		(ambientCol
			));

	float3 halfDirection = normalize(viewDir.xyz + _WorldSpaceLightPos0.xyz);

	float NdotH = max(0.01, (dot(worldNormal, halfDirection)));// *pow(smoothness + 0.2, 8);

	float power = pow(smoothness, 8) * 4096;

	float normTerm = pow(NdotH, power)*power*0.01;

	float3 reflResult = (
		normTerm 

		*_LightColor0*reflectivness +

		ambientRefl.rgb

		)* col.a*fernel;

	col.rgb += reflResult;

	BleedAndBrightness(col, fernel);

}


inline void APPLY_PROJECTED_WATER(float showWater, inout float3 worldNormal, float3 waterNrm, inout float3 tc_Control, float3 waterPos, 
	float viewDirY, inout float4 col, inout float smoothness, inout float ambient, inout float shadow) {

	float deWater = 1 - showWater;

	worldNormal.xyz = waterNrm.xyz * showWater + worldNormal.xyz * deWater;

	tc_Control = WORLD_POS_TO_TERRAIN_UV_3D(waterPos) * showWater + tc_Control * deWater;

	col.rgb  *= saturate(viewDirY)*showWater + deWater;

	smoothness = smoothness * deWater + showWater;

	ambient = ambient * deWater + showWater;

	shadow = shadow * deWater + showWater;


}

inline void Terrain_Water_AndLight(inout float4 col, float3 tc_Control, float ambient, float smoothness, float3 worldNormal, float3 viewDir, float shadow, float Metallic) {

	
	float dotprod = max(0, dot(worldNormal, viewDir.xyz));

	float fernel =  (1.5 - dotprod)*0.66;
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

	float4 terrainAmbient = tex2Dlod(_TerrainColors, float4(tc_Control.xz + worldNormal.xz*0.003, 0, 0)) * inRange; //outRange;

	terrainAmbient.a = tex2Dlod(_TerrainColors, float4(tc_Control.xz, 0, 0)).a * inRange + outRange;

	terrainAmbient.rgb *= teraBounce;
	terrainAmbient.a *= ambient;
	float4 terrainLrefl = tex2Dlod(_TerrainColors, float4(tc_Control.xz - reflected.xz*smoothness*terrainAmbient.a*0.1, 0, 6*deSmoothness))* inRange;

	terrainLrefl.rgb *= teraBounce;

	float3 ambientRefl = ShadeSH9(float4(normalize(-reflected), 1))*terrainAmbient.a;
	float3 ambientCol = ShadeSH9(float4(worldNormal, 1))*terrainAmbient.a;

	_LightColor0 *= direct;

	float deMetalic = (1 - Metallic);



	col.rgb = col.rgb* (_LightColor0 + (terrainAmbient.rgb + ambientCol)*fernel*terrainAmbient.a) *(0.5 + deSmoothness*0.5);

	

	float3 halfDirection = normalize(viewDir.xyz + _WorldSpaceLightPos0.xyz);

	float NdotH = max(0.01, (dot(worldNormal, halfDirection)));
	
	float power = pow(smoothness, 8) * 4096;

	float normTerm = pow(NdotH, power)*power;

	float3 reflResult = (normTerm *_LightColor0 + (terrainLrefl.rgb + ambientRefl.rgb)*ambient)* smoothness;

	col.rgb += reflResult* (deMetalic + col.rgb*Metallic);

	BleedAndBrightness(col, fernel);

}

inline void Terrain_4_Splats(float4 cont, float2 lowtiled, float2 tiled, float far, float deFar, inout float4 terrain, float triplanarY, inout float4 terrainN, inout float maxheight)
{

	float4 lt = float4(lowtiled, 0, getLOD(lowtiled, _mergeSplat_4_TexelSize));
	float4 t = float4(tiled, 0, getLOD(tiled, _mergeSplat_4_TexelSize));

	float4 splat0 = tex2Dlod(_mergeSplat_0, lt)*far + tex2Dlod(_mergeSplat_0, t)*deFar;
	float4 splat1 = tex2Dlod(_mergeSplat_1, lt)*far + tex2Dlod(_mergeSplat_1, t)*deFar;
	float4 splat2 = tex2Dlod(_mergeSplat_2, lt)*far + tex2Dlod(_mergeSplat_2, t)*deFar;
	float4 splat3 = tex2Dlod(_mergeSplat_3, lt)*far + tex2Dlod(_mergeSplat_3, t)*deFar;

	float4 splat0N = tex2Dlod(_mergeSplatN_0, lt)*far + tex2Dlod(_mergeSplatN_0, t)*deFar;
	float4 splat1N = tex2Dlod(_mergeSplatN_1, lt)*far + tex2Dlod(_mergeSplatN_1, t)*deFar;
	float4 splat2N = tex2Dlod(_mergeSplatN_2, lt)*far + tex2Dlod(_mergeSplatN_2, t)*deFar;
	float4 splat3N = tex2Dlod(_mergeSplatN_3, lt)*far + tex2Dlod(_mergeSplatN_3, t)*deFar;

	float newHeight = cont.r * triplanarY + splat0N.b;
	float adiff = max(0, (newHeight - maxheight));
	float alpha = min(1, adiff*(1 + MERGE_POWER*terrain.a*splat0.a));
	float dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat0*alpha;
	terrainN = terrainN*(dAlpha)+splat0N*alpha;
	maxheight += adiff;

	newHeight = cont.g*triplanarY + splat1N.b;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + MERGE_POWER*terrain.a*splat1.a));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat1*alpha;
	terrainN = terrainN*(dAlpha)+splat1N*alpha;
	maxheight += adiff;

	newHeight = cont.b*triplanarY + splat2N.b;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + MERGE_POWER*terrain.a*splat2.a));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat2*alpha;
	terrainN = terrainN*(dAlpha)+splat2N*alpha;
	maxheight += adiff;

	newHeight = cont.a*triplanarY + splat3N.b;
	adiff = max(0, (newHeight - maxheight));
	alpha = min(1, adiff*(1 + MERGE_POWER*terrain.a*splat3.a));
	dAlpha = (1 - alpha);
	terrain = terrain*(dAlpha)+splat3*alpha;
	terrainN = terrainN*(dAlpha)+splat3N*alpha;
	maxheight += adiff;

	terrainN.rg = terrainN.rg * 2 - 1;
}

inline void Terrain_Trilanear(float3 tc_Control, float3 worldPos, float dist, inout float3 worldNormal, inout float4 col, inout float4 terrainN, float4 bumpMap) {

	float far = min(1, dist*0.01);
	float deFar = 1 - far;

	float4 cont = tex2D(_mergeControl, tc_Control.xz);
	float4 height = tex2D(_mergeTerrainHeight, tc_Control.xz + _mergeTerrainScale.w);
	float3 bump = (height.rgb - 0.5) * 2;

	float above = worldPos.y - _mergeTeraPosition.y;

	float aboveTerrainBump = above - height.a*_mergeTerrainScale.y;
	float aboveTerrainBump01 = saturate(aboveTerrainBump);
	float deAboveBump = 1 - aboveTerrainBump01;
	bump = (bump * deAboveBump + worldNormal * aboveTerrainBump01);


	float2 tiled = tc_Control.xz*_mergeTerrainTiling.xy + _mergeTerrainTiling.zw; // -worldNormal.xz*saturate(above - 0.5);
	float tiledY = tc_Control.y * _mergeTeraPosition.w * 2;

	float2 lowtiled = tc_Control.xz*_mergeTerrainTiling.xy*0.1;

	
	float4 splaty = tex2D(_mergeSplat_4, lowtiled);//*far +//tex2D(_mergeSplat_4, tiled)	*deFar;
	float4 splatz = tex2D(_mergeSplat_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_mergeSplat_4, float2(tiled.x, tiledY))*deFar;
	float4 splatx = tex2D(_mergeSplat_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_mergeSplat_4, float2(tiled.y, tiledY))*deFar;


	// Splat 4 is a base layer:
	float4 splatNy = tex2D(_mergeSplatN_4, lowtiled);//*far + tex2D(_mergeSplatN_4, tiled)*deFar;
	float4 splatNz = tex2D(_mergeSplatN_4, float2(tiled.x, tiledY)*0.1)*far + tex2D(_mergeSplatN_4, float2(tiled.x, tiledY))*deFar;
	float4 splatNx = tex2D(_mergeSplatN_4, float2(tiled.y, tiledY)*0.1)*far + tex2D(_mergeSplatN_4, float2(tiled.y, tiledY))*deFar;

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

inline float3 volumeUVtoWorld(float2 uv, float4 VOLUME_POSITION_N_SIZE, float4 VOLUME_H_SLICES) {

	int hy = floor(uv.y*VOLUME_H_SLICES.x);
	int hx = floor(uv.x*VOLUME_H_SLICES.x);

	float2 xz = uv * VOLUME_H_SLICES.x;

	xz.x -= hx;
	xz.y -= hy;

	xz *= VOLUME_H_SLICES.y*2;
	xz -= VOLUME_H_SLICES.y;

	float h = hy * VOLUME_H_SLICES.x + hx;

	float3 bsPos = float3(xz.x, h, xz.y) / VOLUME_POSITION_N_SIZE.w;

	float3 worldPos = VOLUME_POSITION_N_SIZE.xyz + bsPos;

	return worldPos;
}

//  var VOLUME_POSITION_N_SIZE = new Vector4(pos.x, pos.y, pos.z, 1f / size);
//var VOLUME_H_SLICES = new Vector4(slices, w * 0.5f, 1f / ((float)w), 1f / ((float)slices));
//hSlices, w * 0.5f, 1f / w, 1f / hSlices

inline float4 SampleVolume(sampler2D volume, float3 worldPos, float4 VOLUME_POSITION_N_SIZE, float4 VOLUME_H_SLICES, float3 normal) {


	float3 bsPos = (worldPos.xyz - VOLUME_POSITION_N_SIZE.xyz)*VOLUME_POSITION_N_SIZE.w +normal;

	bsPos.xz = saturate((bsPos.xz + VOLUME_H_SLICES.y)* VOLUME_H_SLICES.z)*VOLUME_H_SLICES.w;
	float h = min(max(0, bsPos.y), VOLUME_H_SLICES.x*VOLUME_H_SLICES.x - 1);

	float sectorY = floor(h * VOLUME_H_SLICES.w);
	float sectorX = floor(h - sectorY * VOLUME_H_SLICES.x);

	float2 sector = saturate(float2(sectorX, sectorY)*VOLUME_H_SLICES.w);

	float4 bakeUV = float4(sector + bsPos.xz, 0, 0);
	float4 bake = tex2Dlod(volume, bakeUV);

	h += 1;

	sectorY = floor(h * VOLUME_H_SLICES.w);
	sectorX = floor(h - sectorY * VOLUME_H_SLICES.x);

	sector = saturate(float2(sectorX, sectorY)*VOLUME_H_SLICES.w);

	float4 bakeUp = tex2Dlod(volume, float4(sector + bsPos.xz, 0, 0));

	float deH = frac(h); 

	bake = bake * (1 - deH) + bakeUp * (deH);

	return bake;
}

inline void PointLight(inout float3 scatter, inout float3 glossLight, inout float3 directLight,
float3 vec, float3 normal, float3 viewDir, float ambientBlock, float bake, float directBake, float4 lcol, float power
	) {

	
	float len = length(vec);
	vec /= len;

	float direct = max(0, dot(normal, -vec));
	direct = saturate(direct - ambientBlock * (1 - direct))*directBake; // Multiply by shadow

	float lensq = len * len;
	float3 distApprox = lcol.rgb / lensq;


	float3 halfDirection = normalize(viewDir - vec);
	float NdotH = max(0.01, (dot(normal, halfDirection)));
	float normTerm = pow(NdotH, power); // GGXTerm(NdotH, power);

	scatter += distApprox * bake;
	glossLight += lcol.rgb*normTerm*direct;
	directLight += distApprox * direct;

}

inline void PointLightTransparent(inout float3 scatter, inout float3 directLight,
	float3 vec, float3 viewDir, float ambientBlock, float bake, float directBake, float4 lcol
) {


	float len = length(vec);
	vec /= len;

	float direct = directBake;// saturate(direct - ambientBlock * (1 - direct))*directBake; // Multiply by shadow

	float lensq = len * len;
	float3 distApprox = lcol.rgb / lensq;

	float power = pow(max(0.01, dot(viewDir, vec)), 256*(2.5- directBake));
	//float3 halfDirection = normalize(viewDir - vec);
	//float NdotH = max(0.01, (dot(normal, halfDirection)));
	//float normTerm = pow(NdotH, power); // GGXTerm(NdotH, power);

	scatter += distApprox * bake;
	//glossLight += lcol.rgb*normTerm*direct;
	directLight += (distApprox  + lcol.rgb*power) * direct;

}

inline void DirectionalLightTransparent(inout float3 scatter, inout float3 directLight,
	float shadow, float3 normal, float3 viewDir, float ambientBlock, float bake) {
	
	_LightColor0.rgb *= shadow;

	float dott =  dot(viewDir, -_WorldSpaceLightPos0.xyz);

	float power = pow(max(0.01, dott), 256);

	scatter += ShadeSH9(float4(normal, 1))*bake +power * _LightColor0.rgb*8;
	directLight += _LightColor0.rgb*max(0.1,-dott);

}

inline void DirectionalLight(inout float3 scatter, inout float3 glossLight, inout float3 directLight, 
	float shadow,  float3 normal, float3 viewDir, float ambientBlock, float bake, float power) {
	shadow = saturate(shadow * 2 - ambientBlock);

	float direct = max(0, dot(_WorldSpaceLightPos0, normal));
	
	direct = direct * shadow; // Multiply by shadow

	float3 halfDirection = normalize(viewDir.xyz + _WorldSpaceLightPos0.xyz);
	float NdotH = max(0.01, (dot(normal, halfDirection)));
	float normTerm = pow(NdotH, power);

	_LightColor0.rgb *= direct;

	scatter += ShadeSH9(float4(normal, 1))*bake;
	glossLight += normTerm *_LightColor0.rgb*power*0.1;
	directLight += _LightColor0.rgb;
	

}