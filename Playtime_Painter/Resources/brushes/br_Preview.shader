Shader "Editor/br_Preview" {
		Properties {
		_PreviewTex ("Base (RGB)", 2D) = "white" { }
		_AtlasTextures("_Textures In Row _ Atlas", float) = 1
		}
		Category{
		Tags{ "Queue" = "Geometry"  "RenderType"="Opaque" }
		ColorMask RGBA
		Cull off

		SubShader{
		Pass{

		CGPROGRAM

		#pragma multi_compile  PREVIEW_FILTER_SMOOTH PREVIEW_FILTER_PIXEL
		#pragma multi_compile  PREVIEW_RGB PREVIEW_ALPHA
		#pragma multi_compile  BRUSH_2D  BRUSH_3D BRUSH_DECAL
		#pragma multi_compile  BRUSH_NORMAL BRUSH_ADD BRUSH_SUBTRACT BRUSH_COPY
		#pragma multi_compile  UV_NORMAL UV_ATLASED


		#pragma vertex vert
		#pragma fragment frag

		#include "qc_Includes.cginc"

		
		
		sampler2D _PreviewTex;
		float _AtlasTextures;
		float4 _PreviewTex_ST;
		float4 _PreviewTex_TexelSize;


	
		struct v2f {
		float4 pos : POSITION;
		float4 texcoord : TEXCOORD0;  
		float3 worldPos : TEXCOORD1;
#if UV_ATLASED
		float4 atlasedUV : TEXCOORD2;
#endif
	};

		v2f vert(appdata_full v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);   

			v.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _PreviewTex);

			o.texcoord = previewTexcoord (v.texcoord.xy);


			o.worldPos = mul(unity_ObjectToWorld, v.vertex);

#if UV_ATLASED

			float atlasNumber = v.texcoord.z;
			float atY = floor(atlasNumber / _AtlasTextures);
			float atX = atlasNumber - atY*_AtlasTextures;
			float edge = _PreviewTex_TexelSize.x;

			o.atlasedUV.xy = float2(atX, atY) / _AtlasTextures;				//+edge;
			o.atlasedUV.z = edge;										//(1) / _AtlasTextures - edge * 2;
			o.atlasedUV.w = 1 / _AtlasTextures;
#endif

		return o;
		}


	
	float4 frag(v2f i) : COLOR{

#if UV_ATLASED
		i.texcoord.xy = (frac(i.texcoord.xy)*(i.atlasedUV.w) + i.atlasedUV.xy);
#endif

	 #if BRUSH_COPY
	 	_brushColor = tex2Dlod(_SourceTexture, float4(i.texcoord.xy, 0, 0));
	#endif


	float dist = length(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);

	float4 col = 0;
	float alpha = 1;


	#if PREVIEW_FILTER_PIXEL
		float2 perfTex = (floor(i.texcoord.xy*_PreviewTex_TexelSize.z) + 0.5) * _PreviewTex_TexelSize.x;
		float2 off = (i.texcoord.xy - perfTex);

		float n = max(4,30 - dist); 

		float2 offset = saturate((abs(off) * _PreviewTex_TexelSize.z)*(n*2+2) - n);

		off = off * offset;

		i.texcoord.xy = perfTex  + off;

		col = tex2D(_PreviewTex, i.texcoord.xy);
		
		float2 off2 = i.texcoord.zw*i.texcoord.zw;
		float fromCenter = off2.x+off2.y;
		float gridCircleSize = _brushForm.x*16+4;

		fromCenter =(gridCircleSize - fromCenter)/(gridCircleSize);

		float border = 1 - max(offset.x, offset.y)*pow(max(0,(fromCenter)),16);

		col = col*border + (0.5-col*0.5)*(1-border);

		_brushPointedUV.xy = (floor (_brushPointedUV.xy*_PreviewTex_TexelSize.z)+ 0.5) * _PreviewTex_TexelSize.x;

	#endif

	i.texcoord = previewTexcoord (i.texcoord.xy);



#if PREVIEW_FILTER_SMOOTH
		
	alpha *= checkersFromWorldPosition(i.worldPos.xyz,dist); 

	col =  tex2Dlod(_PreviewTex, float4(i.texcoord.xy, 0, 0));

#endif


	#if BRUSH_3D
          alpha *= prepareAlphaSpherePreview (i.texcoord.xy, i.worldPos);
     #endif

	  #if BRUSH_2D

	  #if PREVIEW_FILTER_SMOOTH
          alpha *= prepareAlphaSmoothPreview (i.texcoord);

		  float differentColor = min(0.5, (abs(col.g-_brushColor.g)+abs(col.r-_brushColor.r)+abs(col.b-_brushColor.b))*8);

		 _brushColor = _brushColor*(differentColor+0.5);

		  #else
		   alpha *= prepareAlphaSquarePreview (i.texcoord);
	#endif

     #endif

	 #if BRUSH_DECAL
	   float2 decalUV = (i.texcoord.xy - _brushPointedUV.xy)*256/_brushForm.y;

	 	float sinX = sin ( _DecalParameters.x );
		float cosX = cos ( _DecalParameters.x );
		float sinY = sin ( _DecalParameters.x );
		float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);

	    decalUV =  mul ( decalUV, rotationMatrix );
      	
	  float Height = tex2D(_VolDecalHeight, decalUV +0.5).a;
	  float4 overlay = tex2D(_VolDecalOverlay, decalUV +0.5);
		float difference = saturate((Height-col.a) * 8*_DecalParameters.y-0.01);

		float changeColor = _DecalParameters.z;

		_brushColor = overlay*overlay.a + (changeColor * _brushColor+ col* (1-changeColor))*(1-overlay.a);

		decalUV = max(0,(abs(decalUV)-0.5));
		alpha *= difference*saturate(1-(decalUV.x+decalUV.y)*999999);
		 
	 #endif


	#if PREVIEW_ALPHA
		//col = src*src*_brushMask+col*col*(1-_brushMask);
		col = col*_brushMask + 0.5*(1 - _brushMask)+col.a*_brushMask.a; //  col.a;
	#endif
	

	 #if BRUSH_NORMAL || BRUSH_COPY
		col = blitWithDestBufferPreview (alpha, _brushColor,  i.texcoord.xy , col);
	#endif

	 #if BRUSH_ADD
		col =  addWithDestBufferPreview (alpha*0.4, _brushColor,  i.texcoord.xy, col);
	#endif
    

     #if BRUSH_SUBTRACT
        col =  subtractFromDestBufferPreview (alpha*0.4, _brushColor,  i.texcoord.xy, col);
    #endif


	return col;

	}
		ENDCG
	}
	}
	}
}