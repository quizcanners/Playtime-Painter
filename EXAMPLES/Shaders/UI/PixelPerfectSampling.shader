Shader "Playtime Painter/UI/PixelPerfectSampling"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Courners("Rounding Courners", Range(0,0.9)) = 0.5
		_Edge("Edge Smoothnesss", Range(0.2,1)) = 0.5
		_ProjTexPos("Screen Space Position", Vector) = (0,0,0,0)
	}
		Category{
		Tags{
		"Queue" = "AlphaTest"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
	}
		ColorMask RGBA
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
		Pass{

		CGPROGRAM

#include "UnityCG.cginc"

#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile_fwdbase
#pragma multi_compile_instancing
#pragma target 3.0

		struct v2f {
		float4 pos : SV_POSITION;
		float3 worldPos : TEXCOORD0;
		float3 normal : TEXCOORD1;
		float4 texcoord : TEXCOORD2;
		float3 viewDir: TEXCOORD4;
		float4 screenPos : TEXCOORD5;
		float4 color: COLOR;
	};


	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	float _Courners;
	float _Edge;
	float4 _ProjTexPos;

	v2f vert(appdata_full v) {
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);
		o.texcoord.xy = v.texcoord.xy;
		o.screenPos = ComputeScreenPos(o.pos);
		o.color = v.color;


		float2 scale = _MainTex_TexelSize.zw;
		o.texcoord.zw = float2 (max(0, (scale.x - scale.y) / scale.x), max(0, (scale.y - scale.x) / scale.y));

		return o;
	}

	

	float4 frag(v2f i) : COLOR{

		// Pixel Pefect Sampling
		float2 screenUV = i.screenPos.xy / i.screenPos.w;

		float2 inPix = (screenUV - _ProjTexPos.xy)*_ScreenParams.xy; 
		float2 texUV = inPix * _MainTex_TexelSize.xy
			// Just in case a texture is not divisible by 2
			+ _MainTex_TexelSize.xy*0.5*(_MainTex_TexelSize.zw % 2);

		float4 col = tex2D(_MainTex, texUV + 0.5);

		// Rounded Courners

		float _Blur = (1 - i.color.a); // UI component data

		float2 uv = abs(i.texcoord.xy - 0.5) * 2;

		uv = max(0, uv - i.texcoord.zw) / (1 - i.texcoord.zw) - _Courners;

		float deCourners = 1 - _Courners;

		uv = max(0, uv) / deCourners;

		uv *= uv;

		col.a = saturate((1 - uv.x - uv.y) * 40 * _Edge * (1 - _Blur)*deCourners);

		return col;
	}
		ENDCG

	}
	}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}

}
