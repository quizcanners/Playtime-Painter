#include "UnityCG.cginc"	
	
static const float GAMMA_TO_LINEAR = 2.2;
static const float LINEAR_TO_GAMMA = 1 / GAMMA_TO_LINEAR;

sampler2D _SourceTexture;
sampler2D _DestBuffer;
float4 _DestBuffer_TexelSize;
sampler2D _SourceMask;
sampler2D _VolDecalHeight;
sampler2D _VolDecalOverlay;
float4 _brushForm; // x - alpha, y - sphere scale, z - uvspace scale
float4 _PaintersSection;
float4 _brushMask;
float4 _brushColor;
float4 _brushWorldPosFrom; 
float4 _brushWorldPosTo; // W - Length
float4 _brushEditedUVoffset;
float4 _maskDynamics;
float4 _maskOffset;
float4 _RTcamPosition;
float4 _brushPointedUV;
float4 _DecalParameters;
float4 _brushAtlasSectionAndRows;
float4 _brushSamplingDisplacement;
float4 _brushPointedUV_Untiled;
float _BufferCopyAspectRatio = 1;


sampler2D pp_DepthProjection;
float4 pp_DepthProjection_TexelSize;
float4x4 pp_ProjectorMatrix;
float4 pp_ProjectorPosition;
float4 pp_ProjectorClipPrecompute;
float4 pp_ProjectorConfiguration;

/*float3 Hue(float H)
{
	float R = abs(H * 6 - 3) - 1;
	float G = 2 - abs(H * 6 - 2);
	float B = 2 - abs(H * 6 - 4);
	return saturate(float3(R, G, B));
}

float4 HSVtoRGB(in float3 HSV)
{
	return float4(((Hue(HSV.x) - 1) * HSV.y + 1) * HSV.z, 1);
}*/


inline float ProjectorSquareAlpha(float4 shadowCoords) {

	float2 xy = abs(shadowCoords.xy);

	return saturate((sign(shadowCoords.w) - max(xy.x,xy.y))*1000);
}

inline float ProjectorCircularAlpha(float4 shadowCoords) {
	return max(0, sign(shadowCoords.w) - dot(shadowCoords.xy, shadowCoords.xy));
}

inline float ProjectorDepthDifference (float4 shadowCoords, float3 worldPos, out float2 pUv) {

		float camAspectRatio = pp_ProjectorConfiguration.x;
		float camFOVDegrees = pp_ProjectorConfiguration.y;
		//float near = pp_ProjectorConfiguration.z;
		float deFar = pp_ProjectorConfiguration.w;

		float viewPos = length(float3(shadowCoords.xy * camFOVDegrees, 1))*camAspectRatio;

		pUv = (shadowCoords.xy + 1) * 0.5;

		float pdist = length(worldPos - pp_ProjectorPosition.xyz);

		float true01Range = pdist * deFar;

		float predictedDepth = 1 - (((viewPos / true01Range) - pp_ProjectorClipPrecompute.y) * pp_ProjectorClipPrecompute.z);

		return 1 - saturate ((tex2D(pp_DepthProjection, pUv).r - predictedDepth) * pdist* pdist * 20);

}

inline float random(float2 st) {
	return frac(sin(dot(st.xy+_Time.x, float2(12.9898f, 78.233f)))* 43758.5453123f);
}

inline bool isAcute(float a, float b, float c) {
    if (c == 0) return true;
    float longest = max(a, b);
    longest *= longest;
    float side = min(a, b);
    return (longest > (c * c + side * side));
}

inline float2 smoothPixelUV (float2 uv, float texSizeX, float texSizeZ, float dist){
	
	float2 perfTex = (floor(uv*texSizeZ) + 0.5) * texSizeX;
	float2 off = (uv - perfTex);

	float n = max(4,30 - dist); 

	float2 offset = saturate((abs(off) *texSizeZ)*(n*2+2) - n);

	off = off * offset;

	return perfTex  + off;
}

inline float checkersFromWorldPosition(float3 worldPos, float distance){
	
	worldPos *= 8;

	float3 awpos  = abs(worldPos);
	int3 iwpos = awpos;
	float3 smooth = abs(awpos-iwpos-0.5);
	float smoothing = max(smooth.x , max(smooth.y, (smooth.z)));
	int ind = (iwpos.x+iwpos.y+iwpos.z);
	int o = ind*0.5f;
	smoothing = saturate((0.499-smoothing)*512/distance);
	return ( 0.5+ abs(ind - o*2)*0.5*smoothing + 0.25*(1-smoothing));
	
}

inline float2 previewTexcoord (float2 uv){
	return (_brushPointedUV.xy - floor(_brushPointedUV.xy) - uv.xy + floor(uv.xy)) / _brushForm.z;
}

inline float4 brushTexcoord (float2 texcoord, float4 vertex){

	float4 tmp;

	tmp.zw = texcoord.xy-0.5;
	float3 worldPos = mul (unity_ObjectToWorld, vertex).xyz-_RTcamPosition.xyz;
	tmp.xy = worldPos.xy / 256;
	tmp.x *= _BufferCopyAspectRatio;
		
	tmp.xy += 0.5;

	return tmp;
}

inline float getMaskedAlpha (float2 texcoord){
	float4 fmask = tex2Dlod(_SourceMask, float4(texcoord.xy*_maskDynamics.x+_maskOffset.xy, 0, 0));

	float mask = fmask.a* (1 - _maskDynamics.w) + fmask.r * _maskDynamics.w;

	return mask * _maskDynamics.z + (1-mask)*(1-_maskDynamics.z);
}

inline float positionToAlpha (float3 worldPos){
  float a = length(_brushWorldPosFrom-worldPos);
          float b = length(_brushWorldPosTo-worldPos);
		  float c = _brushWorldPosTo.w;
		float dist = 0;

        if (isAcute(a, b, c)) dist = min(a, b);
        else {
            float s = (a + b + c) / 2;
            float h = 4 * s * (s - a) * (s - b) * (s - c) / (c * c);
            dist = sqrt(h);
        }
		
		 return (_brushForm.y -  dist)/_brushForm.y; 
}

inline float calculateAlpha (float a, float fromMask){
	float hardmod = _maskDynamics.y/512;
	return saturate(pow( a*(1-hardmod)+(a*(fromMask)*3*hardmod) ,(1+_maskDynamics.y*0.1))*_brushForm.x);
}

inline float prepareAlphaSphere (float2 texcoord, float3 worldPos){
	float mask = getMaskedAlpha (texcoord);

	float alpha = positionToAlpha (worldPos);

	return calculateAlpha (alpha, mask);
}

inline float prepareAlphaSpherePreview (float2 texcoord, float3 worldPos){
	float mask = getMaskedAlpha (texcoord);

	float alpha = max(0, positionToAlpha (worldPos));

	return calculateAlpha (alpha, mask);
}

inline float alphaFromUV (float4 texcoord){
		
	float2 off = texcoord.zw * texcoord.zw;
	float a = off.x+off.y;
		
	return 1 - a*(4);
}

inline float prepareAlphaSquare(float2 texcoord) {
	/*float mask = getMaskedAlpha(texcoord.xy);

	clip(1 - texcoord.z*texcoord.z);
	clip(1 - texcoord.w*texcoord.w);


	return calculateAlpha(1, mask);*/


	float4 tc = float4(texcoord.xy, 0, 0);

	float2 perfTex = (floor(tc.xy*_DestBuffer_TexelSize.z) + 0.5) * _DestBuffer_TexelSize.x;
	float2 off = (tc.xy - perfTex);

	float n = 4;

	float2 offset = saturate((abs(off) * _DestBuffer_TexelSize.z)*(n * 2 + 2) - n);

	off = off * offset;

	tc.xy = perfTex + off;

	tc.zw = previewTexcoord(tc.xy);

	off = tc.zw * tc.zw;
	float a = max(off.x, off.y);

	a =  1 - a * (4);

	return calculateAlpha(1, saturate(a * 128));

}

inline float prepareAlphaSquarePreview(float4 texcoord) {

	float2 off = texcoord.zw * texcoord.zw;
	float a = max(off.x, off.y);

	a = 1 - a * (4);

	return saturate(a * 128);
}

inline float prepareAlphaSmooth (float4 texcoord){
	float mask = getMaskedAlpha (texcoord.xy);

	float a = alphaFromUV (texcoord);

	clip(a);

	return calculateAlpha (a, mask);
}

inline float prepareAlphaSmoothPreview (float4 texcoord){
	float mask = getMaskedAlpha (texcoord.xy);

	float a = max(0, alphaFromUV (texcoord));

	return calculateAlpha (a, mask);
}

inline float4 AlphaBlitTransparent(float alpha, float4 src, float2 texcoord) {
	
	float4 col = tex2Dlod(_DestBuffer, float4(texcoord.xy, 0, 0));

	float rgbAlpha = src.a*alpha;
	
	rgbAlpha = saturate(rgbAlpha * 2 / (col.a + rgbAlpha));

	_brushMask.a *= alpha;

	_brushMask.rgb *= rgbAlpha;

	float4 tmpCol;

	#ifdef UNITY_COLORSPACE_GAMMA
	tmpCol.rgb  = pow(pow(src.rgb, GAMMA_TO_LINEAR)*_brushMask.rgb + pow(col.rgb, GAMMA_TO_LINEAR) *(1 - _brushMask.rgb), LINEAR_TO_GAMMA);
	tmpCol.a = src.a*_brushMask.a + col.a * (1 - _brushMask.a);
	#else 
	tmpCol = src * _brushMask + col * (1 - _brushMask);
	#endif

	col = tmpCol* src.a + (1- src.a)*(float4(col.rgb, col.a*(1-alpha)));

	return  max(0, col);
}

inline float4 AlphaBlitTransparentPreview(float alpha, float4 src, float2 texcoord, float4 col) {
	
	alpha = alpha / min(1, col.a + alpha+0.000000001) * _brushPointedUV.w;

	_brushMask *= alpha;

	float4 tmpCol;

	#ifdef UNITY_COLORSPACE_GAMMA
	tmpCol = pow(src, GAMMA_TO_LINEAR)*_brushMask + pow(col, GAMMA_TO_LINEAR) *(1 - _brushMask);
	tmpCol = pow(tmpCol, LINEAR_TO_GAMMA);
	#else 
	tmpCol = src * _brushMask + col * (1 - _brushMask);
	
	#endif

	col = tmpCol * src.a + (1 - src.a)*(float4(col.rgb, col.a*(1 - alpha)));

	return  col;
}

inline float4 AlphaBlitOpaque (float alpha,float4 src, float2 texcoord){
	_brushMask*=alpha;

	float4 col = tex2Dlod(_DestBuffer, float4(texcoord.xy, 0, 0));

	#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = pow(pow(src.rgb, GAMMA_TO_LINEAR)*_brushMask.rgb + pow(col.rgb, GAMMA_TO_LINEAR) *(1 - _brushMask.rgb), LINEAR_TO_GAMMA);
	col.a = src.a*_brushMask.a + col.a * (1 - _brushMask.a);
	return  max(0, col);
	#else 
	col = src*_brushMask+col*(1-_brushMask);
	return  max(0, col);
	#endif
}

inline float4 AlphaBlitOpaquePreview (float alpha,float4 src, float2 texcoord, float4 col){
	_brushMask*=alpha*_brushPointedUV.w;

	#ifdef UNITY_COLORSPACE_GAMMA
	col = pow(src, GAMMA_TO_LINEAR)*_brushMask+pow(col, GAMMA_TO_LINEAR)*(1-_brushMask);
	return  pow(col, LINEAR_TO_GAMMA);
	#else 
	col = src*_brushMask+col*(1-_brushMask);
	return  col;
	#endif
}

inline float4 addWithDestBuffer (float alpha,float4 src, float2 texcoord){
	_brushMask*=alpha;

	float4 col = tex2Dlod(_DestBuffer, float4(texcoord.xy, 0, 0));

	#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = pow(pow(src.rgb, GAMMA_TO_LINEAR)*_brushMask.rgb+pow(col.rgb, GAMMA_TO_LINEAR), LINEAR_TO_GAMMA);
	col.a = src.a*_brushMask.a + col.a;

	return  col;
	#else 
	col = src*_brushMask+col;
	return  col;
	#endif
}

inline float4 addWithDestBufferPreview (float alpha,float4 src, float2 texcoord, float4 col){
	_brushMask*=alpha*_brushPointedUV.w;

	#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = pow(pow(src.rgb, GAMMA_TO_LINEAR)*_brushMask.rgb+pow(col.rgb, GAMMA_TO_LINEAR), LINEAR_TO_GAMMA);
	col.a += src.a *_brushMask.a;
	return  col;
	#else 
	col = src*_brushMask+col;
	return  col;
	#endif
}

inline float4 subtractFromDestBuffer (float alpha,float4 src, float2 texcoord){
    _brushMask*=alpha;

    float4 col = tex2Dlod(_DestBuffer, float4(texcoord.xy, 0, 0));

    #ifdef UNITY_COLORSPACE_GAMMA
    col.rgb = pow(max(0, pow(col.rgb, GAMMA_TO_LINEAR) - pow(src.rgb, GAMMA_TO_LINEAR) *_brushMask.rgb), LINEAR_TO_GAMMA);
	col.a -= src.a *_brushMask.a;

    return  col;
    #else 
    col = max(0, col - src*_brushMask);
    return  col;
    #endif
}

inline float4 subtractFromDestBufferPreview (float alpha,float4 src, float2 texcoord, float4 col){
    _brushMask*=alpha;

    #ifdef UNITY_COLORSPACE_GAMMA
    col = max(0, pow(col, GAMMA_TO_LINEAR) - pow(src, GAMMA_TO_LINEAR)*_brushMask);
    return  pow(col, LINEAR_TO_GAMMA);
    #else 
    col = max(0, col - src*_brushMask);
    return  col;
    #endif
}