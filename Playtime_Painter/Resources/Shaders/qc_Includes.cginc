#include "UnityCG.cginc"	
	



	sampler2D _SourceTexture;
	sampler2D _DestBuffer;
	sampler2D _SourceMask;
	sampler2D _VolDecalHeight;
    sampler2D _VolDecalOverlay;
    float4 _DestBuffer_TexelSize;
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

	
	inline float4 previewTexcoord (float2 texcoord){
		float4 tmp;
		tmp.xy = texcoord.xy;
		tmp.zw = (_brushPointedUV.xy - texcoord.xy)/_brushForm.z;
		//tmp.zw -= floor(tmp.zw);
		return tmp;
	}

	inline float4 brushTexcoord (float2 texcoord, float4 vertex){

		float4 tmp;


		tmp.zw = texcoord.xy-0.5;
		float3 worldPos = mul (unity_ObjectToWorld, vertex).xyz-_RTcamPosition.xyz;
		tmp.xy = worldPos.xy/256+0.5;

		
		return tmp;
	}

inline float getMaskedAlpha (float2 texcoord){
	float mask = tex2Dlod(_SourceMask, float4(texcoord.xy*_maskDynamics.x+_maskOffset.xy, 0, 0)).a;//_SourceMask
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

inline float prepareAlphaSquare(float4 texcoord) {
	float mask = getMaskedAlpha(texcoord.xy);

	clip(1 - texcoord.z*texcoord.z);
	clip(1 - texcoord.w*texcoord.w);


	return calculateAlpha(1, mask);
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

inline float prepareAlphaSquarePreview (float4 texcoord){
		
		float2 off = texcoord.zw * texcoord.zw;
		float a = max(off.x,off.y);
		
		a = max(0,1 - a*(4));

		return saturate(a*128);
}



inline float4 blitWithDestBuffer (float alpha,float4 src, float2 texcoord){
		_brushMask*=alpha;

		float4 col = tex2Dlod(_DestBuffer, float4(texcoord.xy, 0, 0));


		#ifdef UNITY_COLORSPACE_GAMMA
		col = src*src*_brushMask+col*col*(1-_brushMask);
		return  sqrt(col);
		#else 
		col = src*_brushMask+col*(1-_brushMask);
		return  max(0,col);
		#endif
}

inline float4 blitWithDestBufferPreview (float alpha,float4 src, float2 texcoord, float4 col){
		_brushMask*=alpha*_brushPointedUV.w;

		#ifdef UNITY_COLORSPACE_GAMMA
		col = src*src*_brushMask+col*col*(1-_brushMask);
		return  sqrt(col);
		#else 
		col = src*_brushMask+col*(1-_brushMask);
		return  col;
		#endif
}

inline float4 addWithDestBuffer (float alpha,float4 src, float2 texcoord){
		_brushMask*=alpha;

		float4 col = tex2Dlod(_DestBuffer, float4(texcoord.xy, 0, 0));

		#ifdef UNITY_COLORSPACE_GAMMA
		col = src*src*_brushMask+col*col;
		return  sqrt(col);
		#else 
		col = src*_brushMask+col;
		return  col;
		#endif
}



inline float4 addWithDestBufferPreview (float alpha,float4 src, float2 texcoord, float4 col){
		_brushMask*=alpha*_brushPointedUV.w;

		#ifdef UNITY_COLORSPACE_GAMMA
		col = src*src*_brushMask+col*col;
		return  sqrt(col);
		#else 
		col = src*_brushMask+col;
		return  col;
		#endif
}



inline float4 subtractFromDestBuffer (float alpha,float4 src, float2 texcoord){
        _brushMask*=alpha;

        float4 col = tex2Dlod(_DestBuffer, float4(texcoord.xy, 0, 0));

        #ifdef UNITY_COLORSPACE_GAMMA
        col = max(0,col*col - src*src*_brushMask);
        return  sqrt(col);
        #else 
        col = max(0, col - src*_brushMask);
        return  col;
        #endif
}


inline float4 subtractFromDestBufferPreview (float alpha,float4 src, float2 texcoord, float4 col){
        _brushMask*=alpha;

        #ifdef UNITY_COLORSPACE_GAMMA
        col = max(0,col*col - src*src*_brushMask);
        return  sqrt(col);
        #else 
        col = max(0, col - src*_brushMask);
        return  col;
        #endif
}