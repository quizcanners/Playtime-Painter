Shader "Editor/BlurN_SmudgeBrush" {

		Category{
		Tags{ "Queue" = "Transparent"}

		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
		Pass{

		CGPROGRAM

		#include "qc_Includes.cginc"

		#pragma multi_compile  BRUSH_2D  BRUSH_3D   BRUSH_3D_TEXCOORD2  //BRUSH_DECAL
		#pragma multi_compile  BRUSH_BLUR  BRUSH_BLOOM//BRUSH_NORMAL BRUSH_ADD BRUSH_COPY 

		#pragma vertex vert
		#pragma fragment frag

	#if !(BRUSH_3D || BRUSH_3D_TEXCOORD2)  //BRUSH_2D //|| BRUSH_DECAL
		struct v2f {
		float4 pos : POSITION;
		float4 texcoord : TEXCOORD0;  
	};

		v2f vert(appdata_full v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);   
			o.texcoord = brushTexcoord (v.texcoord.xy, v.vertex);
		return o;
		}
	 #endif

	 #if BRUSH_3D  ||  BRUSH_3D_TEXCOORD2

	struct v2f {
		float4 pos : POSITION;
		float2 texcoord : TEXCOORD0;  
		float3 worldPos : TEXCOORD1;
	};

	v2f vert(appdata_full v) {

		v2f o;
		float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	
#if BRUSH_3D_TEXCOORD2
		v.texcoord.xy = v.texcoord2.xy;
#endif

		// ATLASED CALCULATION
		float atY = floor(v.texcoord.z / _brushAtlasSectionAndRows.z);
		float atX = v.texcoord.z - atY * _brushAtlasSectionAndRows.z;
		v.texcoord.xy = (float2(atX, atY) + v.texcoord.xy) / _brushAtlasSectionAndRows.z
			* _brushAtlasSectionAndRows.w + v.texcoord.xy * (1 - _brushAtlasSectionAndRows.w);


		o.worldPos = worldPos.xyz;

		float2 tmp;

		worldPos.xyz = _RTcamPosition.xyz;
		worldPos.z+=100;
		worldPos.xy+= (v.texcoord.xy*_brushEditedUVoffset.xy+_brushEditedUVoffset.zw-0.5)*256;

		v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz,v.vertex.w));

		o.pos = UnityObjectToClipPos( v.vertex );

		o.texcoord.xy = ComputeScreenPos(o.pos);

		return o;
	}
	#endif
	
	float4 frag(v2f i) : COLOR{

	//_DestBuffer_TexelSize

	float2 uv = i.texcoord.xy;
	float2 d = _DestBuffer_TexelSize.xy*_brushForm.w;



	#if UNITY_COLORSPACE_GAMMA

		float4 col = tex2Dlod(_DestBuffer, float4(uv.x, uv.y, 0, 0));
		_brushColor = col*col;
	  	col = tex2Dlod(_DestBuffer, float4(uv.x+d.x*1.5, uv.y+d.y*0.5, 0, 0));
	  	_brushColor += col*col;
	  	col =  tex2Dlod(_DestBuffer, float4(uv.x-d.x*0.5, uv.y+d.y*1.5, 0, 0));
	  	_brushColor += col*col;
	  	col =  tex2Dlod(_DestBuffer, float4(uv.x-d.x*1.5, uv.y-d.y*0.5, 0, 0));
	  	_brushColor += col*col;
	  	col =  tex2Dlod(_DestBuffer, float4(uv.x+d.x*0.5, uv.y-d.y*1.5, 0, 0));
	  	_brushColor += col*col;

		_brushColor *= 0.2;
	  	_brushColor = sqrt(_brushColor);

		#else 

		_brushColor = tex2Dlod(_DestBuffer, float4(uv.x, uv.y, 0, 0))
	  + tex2Dlod(_DestBuffer, float4(uv.x+d.x*1.5, uv.y+d.y*0.5, 0, 0))
	  + tex2Dlod(_DestBuffer, float4(uv.x-d.x*0.5, uv.y+d.y*1.5, 0, 0))
	  + tex2Dlod(_DestBuffer, float4(uv.x-d.x*1.5, uv.y-d.y*0.5, 0, 0))
	  + tex2Dlod(_DestBuffer, float4(uv.x+d.x*0.5, uv.y-d.y*1.5, 0, 0));
		_brushColor *= 0.2;
		#endif

		

	#if BRUSH_3D || BRUSH_3D_TEXCOORD2
          float alpha = prepareAlphaSphere (i.texcoord, i.worldPos);
		  clip(alpha - 0.000001);
    #endif

	#if BRUSH_2D
          float alpha = prepareAlphaSmooth (i.texcoord);
    #endif

	//#if BRUSH_NORMAL || BRUSH_COPY
	#if BRUSH_BLUR
		return AlphaBlitOpaque (alpha, _brushColor,  i.texcoord.xy);
	#endif

	#if BRUSH_BLOOM
		return addWithDestBuffer (alpha, _brushColor*0.1,  i.texcoord.xy);
	#endif

	}
		ENDCG
	}
	}
	}
}