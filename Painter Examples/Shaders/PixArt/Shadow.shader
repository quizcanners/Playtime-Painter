Shader "Playtime Painter/Pixel Art/Shadow" {
	Properties {
		[NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader {
		Tags {  
			"Queue"="Transparent"
	 		"IgnoreProjector"="True" 
	 		"RenderType"="Transparent" 
		}

		Cull Off
		ZTest On
		Lighting Off
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		float _Glossiness;
		float _Metallic;

		sampler2D _MainTex;
			float4 _MainTex_TexelSize;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {

			float2 perfTex = (floor(IN.uv_MainTex.xy*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;
			float2 off = (IN.uv_MainTex.xy - perfTex);
			off = off * saturate((abs(off) * _MainTex_TexelSize.z) * 40 - 19);
			perfTex += off;

			float4 col=tex2Dlod (_MainTex, float4(perfTex,0,0));

			float4 trans = tex2Dlod (_MainTex, float4(IN.uv_MainTex,0,0));
			
			float diff=saturate(trans.a-col.a);
			col.a =saturate((col.a - 0.1)*512);
			o.Albedo = (col.rgb*max(0,(1-diff*2))+ col.rgb*trans.rgb*diff)*col.a;
			
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			 
			o.Alpha = min(1,col.a+diff*4);

		}
		ENDCG
	} 
	FallBack "Diffuse"
}
