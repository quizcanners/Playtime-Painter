Shader "Brush/br_Multishade" {
	
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

		#pragma multi_compile  BRUSH_2D  BRUSH_3D BRUSH_DECAL
		#pragma multi_compile  BRUSH_NORMAL BRUSH_ADD BRUSH_SUBTRACT BRUSH_COPY

		#pragma vertex vert
		#pragma fragment frag

	#if BRUSH_2D || BRUSH_DECAL
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

	#if BRUSH_3D

	struct v2f {
		float4 pos : POSITION;
		float2 texcoord : TEXCOORD0;  
		float3 worldPos : TEXCOORD1;
	};


	v2f vert(appdata_full v) {

		v2f o;
		float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	
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

	#if BRUSH_COPY
	 	_brushColor = tex2Dlod(_SourceTexture, float4(i.texcoord.xy, 0, 0));
	#endif

	#if BRUSH_3D
        float alpha = prepareAlphaSphere (i.texcoord, i.worldPos);
    #endif

	#if BRUSH_2D
        float alpha = prepareAlphaSmooth (i.texcoord);
    #endif

	#if BRUSH_DECAL
		float2 decalUV =i.texcoord.zw+0.5;
		float Height = tex2D(_VolDecalHeight, decalUV).a;
		float4 overlay = tex2D(_VolDecalOverlay, decalUV);
		float4 dest =  tex2Dlod(_DestBuffer, float4(i.texcoord.xy, 0, 0));
		float alpha = saturate((Height-dest.a) * 8*_DecalParameters.y-0.01);

		float4 col = tex2Dlod(_DestBuffer, float4(i.texcoord.xy, 0, 0));

		float changeColor = _DecalParameters.z;
		_brushColor = overlay*overlay.a +  (_brushColor*changeColor + col*(1-changeColor))*(1-overlay.a);

		//_brushColor = overlay*overlay.a + _brushColor*(1-overlay.a);
		_brushColor.a = Height;
	#endif

	#if BRUSH_NORMAL || BRUSH_COPY
		return blitWithDestBuffer (alpha, _brushColor,  i.texcoord.xy);
	#endif

	#if BRUSH_ADD
		return  addWithDestBuffer (alpha*0.04, _brushColor,  i.texcoord.xy);
	#endif

    #if BRUSH_SUBTRACT
        return  subtractFromDestBuffer (alpha*0.04, _brushColor,  i.texcoord.xy);
    #endif

	}
		ENDCG
	}
	}
	}
}