#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "AutoLight.cginc"

sampler2D _Global_Noise_Lookup;

float4 pp_COLOR_BLEED;

sampler2D _qcPp_mergeControl;

float4 _wrldOffset;

float4 _qcPp_mergeTerrainHeight_TexelSize;

sampler2D _qcPp_mergeTerrainHeight;

float4 _Control_ST;
float4 _qcPp_mergeTerrainTiling;
float4 _qcPp_mergeTeraPosition;
float4 _qcPp_mergeTerrainScale;

sampler2D _qcPp_mergeSplat_0;
sampler2D _qcPp_mergeSplat_1;
sampler2D _qcPp_mergeSplat_2;
sampler2D _qcPp_mergeSplat_3;
sampler2D _qcPp_mergeSplat_4;

float4 _qcPp_mergeSplat_4_TexelSize;

sampler2D _qcPp_mergeSplatN_0;
sampler2D _qcPp_mergeSplatN_1;
sampler2D _qcPp_mergeSplatN_2;
sampler2D _qcPp_mergeSplatN_3;
sampler2D _qcPp_mergeSplatN_4;

uniform sampler2D _qcPp_RayProjectorDepthes;

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


float GetRayTracedShadows(float3 posNrm, float3 norm, float4 shadowCoords, float4 rt_ProjectorConfiguration, float4 rt_ProjectorClipPrecompute, 
	float4 rt_ProjectorPosition, float4 sampleMask
	) { //, float4 shadowCoords1, float4 shadowCoords2 ) {

	float near = rt_ProjectorConfiguration.z;

	//float3 shads;

	float distance = rt_ProjectorClipPrecompute.w;

	float4 shUv = ProjectorUvDepthAlpha(
		shadowCoords, posNrm //+ norm * 0.1 * distance
		,
		rt_ProjectorPosition.rgb,
		rt_ProjectorConfiguration,
		rt_ProjectorClipPrecompute);
	
	const float sharpness = 1024;

	float4 depthAll = tex2Dlod(_qcPp_RayProjectorDepthes, float4(shUv.xy, 0, 0)) * sampleMask;

	float depth = depthAll.r + depthAll.g + depthAll.b + depthAll.a;

	//shads.r = 
	return (1 - saturate((depth - shUv.z) * sharpness / near)) * shUv.w;

	/*near = rt1_ProjectorConfiguration.z;

	distance = rt1_ProjectorClipPrecompute.w;

	float4 shUv1 = ProjectorUvDepthAlpha(
		shadowCoords1, posNrm 
		,
		rt1_ProjectorPosition.rgb,
		rt1_ProjectorConfiguration,
		rt1_ProjectorClipPrecompute);

	depth = tex2Dlod(_qcPp_RayProjectorDepthes, float4(shUv1.xy, 0, 0)).g;

	shads.g = (1 - saturate((depth - shUv1.z) * sharpness / near)) * shUv1.w;

	near = rt2_ProjectorConfiguration.z;

	distance = rt2_ProjectorClipPrecompute.w;

	float4 shUv2 = ProjectorUvDepthAlpha(
		shadowCoords2, posNrm //+ norm * 0.1 * distance
		,
		rt2_ProjectorPosition.rgb,
		rt2_ProjectorConfiguration,
		rt2_ProjectorClipPrecompute);

	depth = tex2Dlod(_qcPp_RayProjectorDepthes, float4(shUv2.xy, 0, 0)).b;

	shads.b = (1 - saturate((depth - shUv2.z) * sharpness / (near * (1- depth)))) * shUv2.w;

	return shads;*/

}

inline void vert_atlasedTexture(float _qcPp_AtlasTextures, float atlasNumber, out float4 atlasedUV) {
	float atY = floor(atlasNumber / _qcPp_AtlasTextures);
	float atX = atlasNumber - atY * _qcPp_AtlasTextures;
	atlasedUV.xy = float2(atX, atY) / _qcPp_AtlasTextures;				
	atlasedUV.z = _qcPp_AtlasTextures;										
	atlasedUV.w = 1 / _qcPp_AtlasTextures;
}

// Old depricated
inline void vert_atlasedTexture(float _qcPp_AtlasTextures, float atlasNumber, float _TexelSizeX, out float4 atlasedUV) {
	float atY = floor(atlasNumber / _qcPp_AtlasTextures);
	float atX = atlasNumber - atY*_qcPp_AtlasTextures;
	atlasedUV.xy = float2(atX, atY) / _qcPp_AtlasTextures;				//+edge;
	atlasedUV.z = _TexelSizeX;										//(1) / _qcPp_AtlasTextures - edge * 2;
	atlasedUV.w = 1 / _qcPp_AtlasTextures;
}

inline float getLOD(float2 uv, float4 _TexelSize) {

	float2 px = _TexelSize.z * abs(ddx(uv.x));
	float2 py = _TexelSize.w * abs(ddy(uv.y));

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
	float2 px = _TexelSize.z * abs(ddx(uv.x));
	float2 py = _TexelSize.w * abs(ddy(uv.y));
	
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

inline float3 GetParallax(float4 tangent, float3 normal, float4 vertex ) {

	float3x3 objectToTangent = float3x3(
		tangent.xyz,
		cross(normal, tangent.xyz) * tangent.w,
		normal
		);

	float3 tangentViewDir = mul(objectToTangent, ObjSpaceViewDir(vertex));

	return tangentViewDir;
}

inline void ApplyTangent (inout float3 normal, float3 tnormal, float4 wTangent){
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


		float2 px = _TexelSize.z * abs(ddx(texcoord.x));
		float2 py = _TexelSize.w * abs(ddy(texcoord.y));



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

/*
inline void leakColors (inout float4 col){

	float3 flow = (col.gbr + col.brg);
	flow *= flow;
	col.rgb += flow*0.02;

}*/

inline void BleedAndBrightness(inout float4 col, float mod, float2 noiseUV) {

	float brightness = pp_COLOR_BLEED.a;

	col.rgb *= 1 + brightness;

	float3 mix = min(col.gbr + col.brg, 128)*mod;
	col.rgb += mix * mix * pp_COLOR_BLEED.r;

	#if USE_NOISE_TEXTURE

	float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(noiseUV * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));

	col.rgb += (noise.rgb - 0.5)*0.05 * col.rgb;

	#endif


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


}

inline float3 volumeUVtoWorld(float2 uv, float4 VOLUME_POSITION_N_SIZE, float4 VOLUME_H_SLICES) {

	// H Slices:
	//hSlices, w * 0.5f, 1f / w, 1f / hSlices

	float hy = floor(uv.y*VOLUME_H_SLICES.x);
	float hx = floor(uv.x*VOLUME_H_SLICES.x);

	float2 xz = uv * VOLUME_H_SLICES.x;

	xz.x -= hx;
	xz.y -= hy;

	xz =  (xz*2.0 - 1.0) *VOLUME_H_SLICES.y;

	//xz *= VOLUME_H_SLICES.y*2;
	//xz -= VOLUME_H_SLICES.y;

	float h = hy * VOLUME_H_SLICES.x + hx;

	float3 bsPos = float3(xz.x, h, xz.y) / VOLUME_POSITION_N_SIZE.w;

	float3 worldPos = VOLUME_POSITION_N_SIZE.xyz + bsPos;

	return worldPos;
}

inline float4 SampleVolume(sampler2D volume, float3 worldPos, float4 VOLUME_POSITION_N_SIZE, float4 VOLUME_H_SLICES) {


	float3 bsPos = (worldPos.xyz - VOLUME_POSITION_N_SIZE.xyz)*VOLUME_POSITION_N_SIZE.w;

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
float3 vec, float3 normal, float3 viewDir, float ambientBlock, float bake, float directBake, float4 lcol, float power) 
{
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
	float3 vec, float3 viewDir, float ambientBlock, float bake, float directBake, float4 lcol) 
{

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