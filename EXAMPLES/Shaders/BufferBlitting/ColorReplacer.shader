Shader "Playtime Painter/Buffer Blit/Color Replacer" {
	Properties{
		_MainTex("Tex", 2D) = "white" {}
		_Color("New Color", Color) = (1,1,1,1)
		_AlphaMask("Alpha Mask", 2D) = "white" {}
		_ColorMask("Color Mask", 2D) = "white" {}
		_AvarageColor("Avarage", Color) = (1,1,1,1)

	}
		Category{
		Tags{ "Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"LightMode" = "ForwardBase"
	}

		ColorMask RGBA
		Cull Back
		ZTest off
		ZWrite off


		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"	

		sampler2D _MainTex;
		sampler2D _AlphaMask;
		sampler2D _ColorMask;
		float4 _Color;
		float4 _AvarageColor;
		float4 _AlphaMask_TexelSize;

	
	//float4 _MainTex_TexelSize;

	struct v2f {
		float4 pos : POSITION;
		float2 texcoord : TEXCOORD0;
	};


	v2f vert(appdata_full v) {
		v2f o;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.texcoord = v.texcoord.xy;

		return o;
	}


	float Contrast(float4 col) {
		float avg = (col.r + col.g + col.b) *0.333;

		float len = abs((col.r - avg)*(col.b - avg)*(col.g - avg));

		return len * col.a;
	}

	float4 frag(v2f i) : COLOR{


		float2 duv = i.texcoord.xy;

		float4 src = tex2D(_MainTex, float2(1-duv.x, duv.y));
		float4 avg = tex2D(_ColorMask, duv);
		float alpha = tex2D(_AlphaMask, duv).a*avg.a;
		


	/*	_AlphaMask_TexelSize.xy *= 2;

		duv = i.texcoord.xy;
		duv.x += _AlphaMask_TexelSize.x;
		float dcol_0 = tex2Dlod(_AlphaMask, float4(duv, 0, 0)).a;

		duv.x -= _AlphaMask_TexelSize.x * 2;
		float dcol_1 = tex2Dlod(_AlphaMask, float4(duv, 0, 0)).a;

		duv.x += _AlphaMask_TexelSize.x;
		duv.y += _AlphaMask_TexelSize.y;
		float dcol_2 = tex2Dlod(_AlphaMask, float4(duv, 0, 0)).a;

		duv.y -= _AlphaMask_TexelSize.y * 2;
		float dcol_3 = tex2Dlod(_AlphaMask, float4(duv, 0, 0)).a;

		
		alpha = (alpha + dcol_0 + dcol_1 + dcol_2 + dcol_3)/5;*/


		avg.rgb = _AvarageColor.rgb;

		float3 diff = src.rgb - avg.rgb;

		float add = length(max(0, diff))/3;

	

		float3 darker =  1- (-min(diff, 0))/ avg.rgb;

		_Color.rgb *= darker;


		//float addDiffuse = normalize(avg)*length(add)*0.33;

		//_Color.rgb *= (1 + addDiffuse);




		_Color.rgb *= (1+length(add)*0.33);

		//alpha = saturate( pow(alpha, 3) * 2);// saturate((alpha - 0.5) * 2);

		src.rgb = src.rgb*(1 - alpha) + _Color * alpha;
		


		return  src;
	}
		ENDCG
	}
	}
	}
}
