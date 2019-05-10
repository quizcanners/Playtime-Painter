Shader "Playtime Painter/Geometry/SamplingDisplacement" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset]_ReshapeTex("Reshape Mask", 2D) = "grey" {}
		_Reshape ("Reshape Mask Size", Vector) = (4,4,0,0)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _ReshapeTex;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		float4 _MainTex_TexelSize;
		float2 _Reshape;

		UNITY_INSTANCING_BUFFER_START(Props)

		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {

			float2 deRS = 1 / _Reshape;

			float2 pix = IN.uv_MainTex*_MainTex_TexelSize.zw;

			float4 r = tex2D(_ReshapeTex, pix * deRS);

			float4 c = tex2D (_MainTex, IN.uv_MainTex + (r.rg-0.5)*2*_Reshape*_MainTex_TexelSize.xy);
			
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
