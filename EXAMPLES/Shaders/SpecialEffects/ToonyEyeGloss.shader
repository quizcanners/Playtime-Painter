// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Painter_Experimental/EyeGloss" {
	Properties{

	}


		Category{
		Tags{ "RenderType" = "Transparent"
		"LightMode" = "ForwardBase"
		"Queue" = "Overlay+10"
	}
		LOD 200
		ColorMask RGBA
		ZWrite Off
		Blend SrcAlpha One

		SubShader{
		Pass{



		CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "AutoLight.cginc"


		struct v2f {
		float4 pos : POSITION;
		float3 viewDir : TEXCOORD0;
		float3 normal : TEXCOORD1;
		SHADOW_COORDS(2)
	};

	v2f vert(appdata_full v) {
		v2f o;

	
		o.pos = UnityObjectToClipPos(v.vertex);
		o.viewDir.xyz = (WorldSpaceViewDir(v.vertex));
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);
	
		TRANSFER_SHADOW(o);
		return o;
	}



	float4 frag(v2f i) : COLOR{

	
	i.viewDir.xyz = normalize(i.viewDir.xyz);

	float dotprod = dot(i.viewDir.xyz, i.normal.xyz);					 
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal.xyz);
	float dott = max(0.01, dot(_WorldSpaceLightPos0, -reflected));

	float shadow = SHADOW_ATTENUATION(i);

	float bright = ( pow(dott,128)*16 * shadow);






	return bright;

	}


		ENDCG
	}
		//UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}
