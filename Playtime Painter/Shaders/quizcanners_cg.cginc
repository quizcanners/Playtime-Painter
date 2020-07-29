
uniform sampler2D _Global_Noise_Lookup;

uniform float4 pp_COLOR_BLEED;

uniform sampler2D _qcPp_mergeControl;

uniform float4 _wrldOffset;

uniform float4 _qcPp_mergeTerrainHeight_TexelSize;

uniform sampler2D _qcPp_mergeTerrainHeight;

uniform float4 _Control_ST;
uniform float4 _qcPp_mergeTerrainTiling;
uniform float4 _qcPp_mergeTeraPosition;
uniform float4 _qcPp_mergeTerrainScale;

uniform sampler2D _qcPp_mergeSplat_0;
uniform sampler2D _qcPp_mergeSplat_1;
uniform sampler2D _qcPp_mergeSplat_2;
uniform sampler2D _qcPp_mergeSplat_3;
uniform sampler2D _qcPp_mergeSplat_4;

uniform float4 _qcPp_mergeSplat_4_TexelSize;

uniform sampler2D _qcPp_mergeSplatN_0;
uniform sampler2D _qcPp_mergeSplatN_1;
uniform sampler2D _qcPp_mergeSplatN_2;
uniform sampler2D _qcPp_mergeSplatN_3;
uniform sampler2D _qcPp_mergeSplatN_4;

uniform sampler2D _qcPp_RayProjectorDepthes;

uniform float4x4 rt0_ProjectorMatrix;
uniform float4 rt0_ProjectorPosition;
uniform float4 rt0_ProjectorClipPrecompute;
uniform float4 rt0_ProjectorConfiguration;

uniform float4x4 rt1_ProjectorMatrix;
uniform float4 rt1_ProjectorPosition;
uniform float4 rt1_ProjectorClipPrecompute;
uniform float4 rt1_ProjectorConfiguration;

uniform float4x4 rt2_ProjectorMatrix;
uniform float4 rt2_ProjectorPosition;
uniform float4 rt2_ProjectorClipPrecompute;
uniform float4 rt2_ProjectorConfiguration;


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

// from http://www.java-gaming.org/index.php?topic=35123.0
float4 cubic_Interpolation(float v) {
	float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
	float4 s = n * n * n;
	float x = s.x;
	float y = s.y - 4.0 * s.x;
	float z = s.z - 4.0 * s.y + 6.0 * s.x;
	float w = 6.0 - x - y - z;
	return float4(x, y, z, w) * (1.0 / 6.0);
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


	

	float2 posToUvUnclamped = (bsPos.xz + VOLUME_H_SLICES.y)* VOLUME_H_SLICES.z;

	bsPos.xz = saturate(posToUvUnclamped);
	
	float2 diff = abs(posToUvUnclamped - bsPos.xz);
	float outOfBounds = diff.x + diff.y;

	bsPos.xz *= VOLUME_H_SLICES.w;

	

	float h = clamp(bsPos.y, 0, VOLUME_H_SLICES.x*VOLUME_H_SLICES.x - 1);
		//min(max(0, bsPos.y), VOLUME_H_SLICES.x*VOLUME_H_SLICES.x - 1);

	outOfBounds += abs(h - bsPos.y);

	float sectorY = floor(h * VOLUME_H_SLICES.w);
	float sectorX = floor(h - sectorY * VOLUME_H_SLICES.x);

	float2 sectorUnclamped = float2(sectorX, sectorY)*VOLUME_H_SLICES.w;

	float2 sector = saturate(sectorUnclamped);

	

	float4 bakeUV = float4(sector + bsPos.xz, 0, 0);
	float4 bake = tex2Dlod(volume, bakeUV);

	h += 1;

	sectorY = floor(h * VOLUME_H_SLICES.w);
	sectorX = floor(h - sectorY * VOLUME_H_SLICES.x);

	sectorUnclamped = float2(sectorX, sectorY)*VOLUME_H_SLICES.w;

	sector = saturate(sectorUnclamped);

	float4 bakeUp = tex2Dlod(volume, float4(sector + bsPos.xz, 0, 0));

	float deH = frac(h); 

	bake = bake * (1 - deH) + bakeUp * (deH);

	float isIn = 1 - saturate(outOfBounds * 999);

	bake.a *= isIn;

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