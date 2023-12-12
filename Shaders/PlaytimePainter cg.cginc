
#include "UnityCG.cginc"

static const float GAMMA_TO_LINEAR = 2.2;
static const float LINEAR_TO_GAMMA = 1 / GAMMA_TO_LINEAR;

uniform float4 _qcPp_TargetTexture_TexelSize;

uniform sampler2D _qcPp_SourceTexture;
uniform float4 _qcPp_SourceTexture_TexelSize;
uniform float4 _qcPp_srcTextureUsage;
uniform sampler2D _qcPp_TransparentLayerUnderlay;
uniform sampler2D _qcPp_AlphaBuffer;
uniform float4 _qcPp_AlphaBuffer_TexelSize;
uniform float4 _qcPp_AlphaBufferCfg;

uniform float4 _qcPp_BufferSourceTexelSize;

uniform sampler2D _qcPp_DestBuffer;
uniform sampler2D _qcPp_SourceMask;
uniform sampler2D _VolDecalHeight;
uniform sampler2D _VolDecalOverlay;
uniform float4 _qcPp_brushForm; // x - alpha, y - sphere scale, z - uvspace scale
uniform float4 _PaintersSection;
uniform float4 _qcPp_brushMask;
uniform float4 _qcPp_brushColor;
uniform float4 _qcPp_brushWorldPosFrom;
uniform float4 _qcPp_brushWorldPosTo; // W - Length
uniform float4 _qcPp_brushEditedUVoffset;
uniform float4 _qcPp_maskDynamics;
uniform float4 _qcPp_maskOffset;
uniform float4 _qcPp_RTcamPosition;
uniform float4 _qcPp_brushUvPosFrom;
uniform float4 _qcPp_brushUvPosTo;
uniform float4 _DecalParameters;
uniform float4 _qcPp_brushAtlasSectionAndRows;
uniform float4 _qcPp_brushSamplingDisplacement;
uniform float4 _qcPp_brushUvPosTo_Untiled;
uniform float4 _qcPp_ChannelSourceMask;
uniform float _qcPp_BufferCopyAspectRatio = 1;

uniform sampler2D pp_DepthProjection;
uniform float4 pp_DepthProjection_TexelSize;

uniform float4x4 pp_ProjectorMatrix;
uniform float4 pp_ProjectorPosition;
uniform float4 pp_ProjectorClipPrecompute;
uniform float4 pp_ProjectorConfiguration;


struct appdata_brush_qc
{
	float4 vertex    : POSITION;  // The vertex position in model space.
	float4 texcoord  : TEXCOORD0; // The first UV coordinate.
	float4 texcoord1  : TEXCOORD1; // The second UV coordinate.
};

#define TRANSFORM_TEX_QC(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)

inline float3 SourceTextureByBrush(float3 src) {

	// 0 - Copy, 1 = Multiply, 2 = Use Brush Color

	float par = _qcPp_srcTextureUsage.x;

	return src*max(0, 1 - par) + _qcPp_brushColor.rgb*max(0, par - 1)
		+ _qcPp_brushColor.rgb*src*(1 - abs(par - 1))
		;
}

inline float ProjectorSquareAlpha(float4 shadowCoords) {

	float2 xy = abs(shadowCoords.xy);

	return saturate((sign(shadowCoords.w) - max(xy.x,xy.y))*1000);
}

inline float ProjectorCircularAlpha(float4 shadowCoords) {
	return max(0, sign(shadowCoords.w) - dot(shadowCoords.xy, shadowCoords.xy));
}

inline float BrushClamp(float2 uv) {
	float doClamp = _qcPp_srcTextureUsage.y;

	uv = abs(uv - 0.5);

	return saturate(1 + (0.5 - max(uv.x, uv.y))*doClamp*200);
}

inline float ProjectorDepthDifference (float4 shadowCoords, float3 worldPos, float2 pUv) {

		float camAspectRatio = pp_ProjectorConfiguration.x;
		float camFOVDegrees = pp_ProjectorConfiguration.y;
		//float near = pp_ProjectorConfiguration.z;
		float deFar = pp_ProjectorConfiguration.w;

		float viewPos = length(float3(shadowCoords.xy * camFOVDegrees, 1))*camAspectRatio;

		//pUv = (shadowCoords.xy + 1) * 0.5;

		float pdist = length(worldPos - pp_ProjectorPosition.xyz);

		float true01Range = pdist * deFar;

		float predictedDepth = 1 - (((viewPos / true01Range) - pp_ProjectorClipPrecompute.y) * pp_ProjectorClipPrecompute.z);

		return 1 - saturate ((tex2D(pp_DepthProjection, pUv).r - predictedDepth) * pdist* pdist * 20);

}

inline float random(float2 st) 
{
	return frac(sin(dot(st.xy+_Time.x, float2(12.9898f, 78.233f)))* 43758.5453123f);
}

inline bool isAcute(float a, float b, float c) 
{
    if (c < 0.0001) return true;
    float longest = max(a, b);
    longest *= longest;
    float side = min(a, b);
    return (longest > (c * c + side * side));
}

inline float2 smoothPixelUV (float2 uv, float texSizeX, float texSizeZ, float dist)
{
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

inline float2 previewTexcoord (float2 uv)
{
	return (_qcPp_brushUvPosTo.xy - floor(_qcPp_brushUvPosTo.xy) - uv.xy + floor(uv.xy)) / _qcPp_brushForm.z;
}

inline float4 brushTexcoord (float2 texcoord, float4 vertex){

	float4 tmp;

	tmp.zw = texcoord.xy-0.5;
	float3 worldPos = mul (unity_ObjectToWorld, vertex).xyz-_qcPp_RTcamPosition.xyz;
	tmp.xy = worldPos.xy / 256;
	tmp.x *= _qcPp_BufferCopyAspectRatio;
		
	tmp.xy += 0.5;

	return tmp;
}

inline float getMaskedAlpha (float2 texcoord){
	float4 fmask = tex2Dlod(_qcPp_SourceMask, float4(texcoord.xy*_qcPp_maskDynamics.x+_qcPp_maskOffset.xy, 0, 0));

	float mask = lerp(fmask.a, fmask.r, _qcPp_maskDynamics.w);//fmask.a* (1 - _qcPp_maskDynamics.w) + fmask.r * _qcPp_maskDynamics.w;

	return lerp((1 - _qcPp_maskDynamics.z), _qcPp_maskDynamics.z, mask);//mask * _qcPp_maskDynamics.z + (1-mask)*(1-_qcPp_maskDynamics.z);
}

inline float DistToLine(float3 pos, float3 a, float3 b) {
	float3 pa = pos - a;
	float3 ba = b - a;
	float t = saturate(dot(pa, ba)/dot(ba,ba));
	return length(pa - ba * t);
}

inline float positionToAlpha(float3 worldPos) 
{
	float dist = DistToLine(worldPos, _qcPp_brushWorldPosFrom, _qcPp_brushWorldPosTo);
	return (_qcPp_brushForm.y - dist) / _qcPp_brushForm.y;
}
/*
inline float POINT_BRUSH_ALPHA_DIRECTED (float3 worldPos, float3 directionForHalfSphere) {

	float3 diff = worldPos - _qcPp_brushWorldPosTo;

    float dist = length(diff);
		
	return (_qcPp_brushForm.y -  dist)/_qcPp_brushForm.y * saturate(dot(normalize(diff) , directionForHalfSphere) * 16);
}*/

inline float alphaFromUV(float4 texcoord) {

	float2 off = texcoord.zw * texcoord.zw;
	float a = off.x + off.y;

	return 1 - a * (4);
}

inline float calculateAlpha (float a, float fromMask)
{
	return smoothstep(0,1, a * fromMask 
		* _qcPp_maskDynamics.y // hardness
	) * _qcPp_brushForm.x;
	/*return saturate(
		pow( a*(1-hardmod) 
		+ (a * fromMask * 3 *hardmod) 
		,
		(1+_qcPp_maskDynamics.y*0.1))

		*_qcPp_brushForm.x/(1 + pow(_qcPp_maskDynamics.y,2))
	);*/
}

inline float SampleAlphaBuffer(float2 uv) {

	float2 off = _qcPp_AlphaBuffer_TexelSize.xy*1.5;

	#define GRABPIXELA(ker) tex2Dlod(_qcPp_AlphaBuffer, float4(uv + ker * off ,0,0)).a

	float alpha =

		max(
			GRABPIXELA(float2(0, 0)),
			max(
				max(
					max(GRABPIXELA(float2(-1, 0)), GRABPIXELA(float2(0, -1))),
					max(GRABPIXELA(float2(1, 0)), GRABPIXELA(float2(0, 1)))
				),
				max(
					max(GRABPIXELA(float2(-1, -1)), GRABPIXELA(float2(1, 1))),
					max(GRABPIXELA(float2(-1, 1)), GRABPIXELA(float2(1, -1)))
				)
			)
		);

	alpha = min(alpha, _qcPp_AlphaBufferCfg.x);

	return alpha;
}

inline float4 GetMaxByAlpha(float4 a, float4 b) {

	float alA = saturate(a.a * 5096 - b.a);

	return lerp(b, a, alA); // a* alA + b * (1 - alA);

}

inline float4 SampleUV_AlphaBuffer(float2 uv) {

	float2 off = _qcPp_AlphaBuffer_TexelSize.xy*1.5;

	#define GRABPIXEL(ker) tex2Dlod(_qcPp_AlphaBuffer, float4(uv + ker * off ,0,0))
	
	#define GETMAXBYALPHA(a, b) 

	float4 result =

		GetMaxByAlpha(
			GRABPIXEL(float2(0, 0)),
			GetMaxByAlpha(
				GetMaxByAlpha(
					GetMaxByAlpha(GRABPIXEL(float2(-1, 0)), GRABPIXEL(float2(0, -1))),
					GetMaxByAlpha(GRABPIXEL(float2(1, 0)), GRABPIXEL(float2(0, 1)))
				),
				GetMaxByAlpha(
					GetMaxByAlpha(GRABPIXEL(float2(-1, -1)), GRABPIXEL(float2(1, 1))),
					GetMaxByAlpha(GRABPIXEL(float2(-1, 1)), GRABPIXEL(float2(1, -1)))
				)
			)
		);

	result.a = min(result.a, _qcPp_AlphaBufferCfg.x);

	return result;
}


inline float prepareAlphaSphere (float2 texcoord, float3 worldPos){
	float mask = getMaskedAlpha(texcoord);

	float alpha = positionToAlpha (worldPos);

	return calculateAlpha (alpha, mask);
}

inline float prepareAlphaSpherePreview (float2 texcoord, float3 worldPos){
	float mask = getMaskedAlpha (texcoord);

	float alpha = max(0, positionToAlpha (worldPos));

	return calculateAlpha (alpha, mask);
}

inline float prepareAlphaSquare(float2 texcoord) {

	float4 tc = float4(texcoord.xy, 0, 0);

	float2 perfTex = (floor(tc.xy*_qcPp_TargetTexture_TexelSize.z) + 0.5) * _qcPp_TargetTexture_TexelSize.x;
	float2 off = (tc.xy - perfTex);

	float n = 4;

	float2 offset = saturate((abs(off) * _qcPp_TargetTexture_TexelSize.z)*(n * 2 + 2) - n);

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

inline float prepareAlphaSmooth (float4 texcoord)
{
	float mask = getMaskedAlpha (texcoord.xy);
	float a = alphaFromUV (texcoord);
	clip(a);
	return calculateAlpha (a, mask);
}

inline float prepareAlphaSmoothPreview (float4 texcoord)
{
	float mask = getMaskedAlpha (texcoord.xy);
	float a = max(0, alphaFromUV (texcoord));
	return calculateAlpha (a, mask);
}

inline float4 BrushMaskWithAlphaBuffer(float alpha, float2 uv, float srcA) {
	
	float ignoreSrcAlpha = _qcPp_srcTextureUsage.w;
	float usingAlphaBuffer = _qcPp_AlphaBufferCfg.z;

	return _qcPp_brushMask * min(_qcPp_AlphaBufferCfg.x * (ignoreSrcAlpha + srcA * (1 - ignoreSrcAlpha)), alpha*_qcPp_brushUvPosTo.w + tex2D(_qcPp_AlphaBuffer, uv).a*usingAlphaBuffer);
}


inline float4 AlphaBlitTransparent(float alpha, float4 src, float2 texcoord) {
	
	float4 col = tex2Dlod(_qcPp_DestBuffer, float4(texcoord.xy, 0, 0));

	float rgbAlpha = src.a*alpha;
	
	rgbAlpha = saturate(rgbAlpha * 2 / (col.a + rgbAlpha));

	_qcPp_brushMask.a *= alpha;

	_qcPp_brushMask.rgb *= rgbAlpha;

	float4 tmpCol;

	#ifdef UNITY_COLORSPACE_GAMMA
	tmpCol.rgb  = pow(
		pow(src.rgb, GAMMA_TO_LINEAR)*_qcPp_brushMask.rgb + pow(col.rgb, GAMMA_TO_LINEAR) *(1 - _qcPp_brushMask.rgb)
		
		, LINEAR_TO_GAMMA);
	tmpCol.a = src.a*_qcPp_brushMask.a + col.a * (1 - _qcPp_brushMask.a);
	#else 
	tmpCol = src * _qcPp_brushMask + col * (1 - _qcPp_brushMask);
	#endif

	col = tmpCol* src.a + (1- src.a)*(float4(col.rgb, col.a*(1-alpha)));

	return  max(0, col);
}

inline float4 AlphaBlitTransparentPreview(float alpha, float4 src, float2 texcoord, float4 col, float srcAlpha) {
	
	alpha = min(1, alpha / min(1, col.a + alpha+0.000000001) * _qcPp_brushUvPosTo.w + tex2D(_qcPp_AlphaBuffer, texcoord).a);

	_qcPp_brushMask *= alpha;

	float ignoreSrcAlpha = _qcPp_srcTextureUsage.w;

	_qcPp_brushMask *= ignoreSrcAlpha + srcAlpha *(1 - ignoreSrcAlpha);

	float4 tmpCol;

	#ifdef UNITY_COLORSPACE_GAMMA
		tmpCol = pow(src, GAMMA_TO_LINEAR)*_qcPp_brushMask + pow(col, GAMMA_TO_LINEAR) *(1 - _qcPp_brushMask);
		tmpCol = pow(tmpCol, LINEAR_TO_GAMMA);
	#else 
		tmpCol = src * _qcPp_brushMask + col * (1 - _qcPp_brushMask);
	#endif

	col = tmpCol * src.a + (1 - src.a)*(float4(col.rgb, col.a*(1 - alpha)));

	return  col;
}

inline float4 AlphaBlitOpaque (float alpha,float4 src, float2 texcoord){
	_qcPp_brushMask*=alpha;

	float4 col = tex2Dlod(_qcPp_DestBuffer, float4(texcoord.xy, 0, 0));

	#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = pow(pow(src.rgb, GAMMA_TO_LINEAR)*_qcPp_brushMask.rgb + pow(col.rgb, GAMMA_TO_LINEAR) *(1 - _qcPp_brushMask.rgb), LINEAR_TO_GAMMA);
	col.a = src.a*_qcPp_brushMask.a + col.a * (1 - _qcPp_brushMask.a);
	return  max(0, col);
	#else 
	col = src*_qcPp_brushMask+col*(1-_qcPp_brushMask);
	return  max(0, col);
	#endif
}

inline float4 AlphaBlitOpaquePreview (float alpha,float4 src, float2 texcoord, float4 col, float srcAlpha){
	_qcPp_brushMask = BrushMaskWithAlphaBuffer(alpha, texcoord, srcAlpha);

	#ifdef UNITY_COLORSPACE_GAMMA
	col = pow(src, GAMMA_TO_LINEAR)*_qcPp_brushMask+pow(col, GAMMA_TO_LINEAR)*(1-_qcPp_brushMask);
	return  pow(col, LINEAR_TO_GAMMA);
	#else 
	col = src*_qcPp_brushMask+col*(1-_qcPp_brushMask);
	return  col;
	#endif
}

inline float4 addWithDestBuffer (float alpha,float4 src, float2 texcoord){
	_qcPp_brushMask*=alpha;

	float4 col = tex2Dlod(_qcPp_DestBuffer, float4(texcoord.xy, 0, 0));

	#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = pow(pow(src.rgb, GAMMA_TO_LINEAR)*_qcPp_brushMask.rgb+pow(col.rgb, GAMMA_TO_LINEAR), LINEAR_TO_GAMMA);
	col.a = src.a*_qcPp_brushMask.a + col.a;

	return  col;
	#else 
	col = src*_qcPp_brushMask+col;
	return  col;
	#endif
}

inline float4 addWithDestBufferPreview (float alpha,float4 src, float2 texcoord, float4 col, float srcAlpha){
	_qcPp_brushMask = BrushMaskWithAlphaBuffer(alpha, texcoord, srcAlpha); //_qcPp_brushMask*=alpha*_qcPp_brushUvPosTo.w;

	#ifdef UNITY_COLORSPACE_GAMMA
	col.rgb = pow(pow(src.rgb, GAMMA_TO_LINEAR)*_qcPp_brushMask.rgb+pow(col.rgb, GAMMA_TO_LINEAR), LINEAR_TO_GAMMA);
	col.a += src.a *_qcPp_brushMask.a;
	return  col;
	#else 
	col = src*_qcPp_brushMask+col;
	return  col;
	#endif
}

inline float4 subtractFromDestBuffer (float alpha,float4 src, float2 texcoord){
    _qcPp_brushMask*=alpha;

    float4 col = tex2Dlod(_qcPp_DestBuffer, float4(texcoord.xy, 0, 0));

    #ifdef UNITY_COLORSPACE_GAMMA
    col.rgb = pow(max(0, pow(col.rgb, GAMMA_TO_LINEAR) - pow(src.rgb, GAMMA_TO_LINEAR) *_qcPp_brushMask.rgb), LINEAR_TO_GAMMA);
	col.a -= src.a *_qcPp_brushMask.a;

    return  col;
    #else 
    col = max(0, col - src*_qcPp_brushMask);
    return  col;
    #endif
}

inline float4 subtractFromDestBufferPreview (float alpha,float4 src, float2 texcoord, float4 col){
	_qcPp_brushMask = BrushMaskWithAlphaBuffer(alpha, texcoord, src.a); //_qcPp_brushMask*=alpha;

    #ifdef UNITY_COLORSPACE_GAMMA
    col = max(0, pow(col, GAMMA_TO_LINEAR) - pow(src, GAMMA_TO_LINEAR)*_qcPp_brushMask);
    return  pow(col, LINEAR_TO_GAMMA);
    #else 
    col = max(0, col - src*_qcPp_brushMask);
    return  col;
    #endif
}