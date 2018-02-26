﻿// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Painter_Experimental/DestructibleEnvironment" {
	Properties {
	[NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
	_Diffuse("Diffuse", 2D) = "white" {}
	_DamDiffuse("Damaged Diffuse", 2D) = "white" {}
	_DamDiffuse2("Damaged Diffuse Deep", 2D) = "white" {}
	[NoScaleOffset]_Bump("Bump", 2D) = "white" {}
	[NoScaleOffset]_BumpD("Bump Damage", 2D) = "white" {}
	[NoScaleOffset]_BumpD2("Bump Damage 2", 2D) = "white" {}
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

		struct Input {
			float2 uv2_MainTex;
			float2 uv_Diffuse;
			float2 uv_DamDiffuse;
			float2 uv_DamDiffuse2;
			float3 viewDir;
		};


		sampler2D _Diffuse;
		sampler2D _DamDiffuse;
		sampler2D _DamDiffuse2;
		sampler2D _Bump;
		sampler2D _BumpD;
		sampler2D _BumpD2;
		float4 _MainTex_TexelSize;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input i, inout SurfaceOutputStandard o) {
			float4 mask = tex2D(_MainTex, i.uv2_MainTex);

			//float2 offsetMainTex = ParallaxOffset(-mask.r, 0.1, i.viewDir)*mask.r;

		//	i.uv2_MainTex += offsetMainTex;
			//mask = tex2D(_MainTex, i.uv2_MainTex);
			clip(0.9 - mask.r);
		

			float maskr = tex2D(_MainTex, float2(i.uv2_MainTex.x,i.uv2_MainTex.y + _MainTex_TexelSize.y*2 )).r;
			float maskg = tex2D(_MainTex, float2(i.uv2_MainTex.x + _MainTex_TexelSize.x*2 , i.uv2_MainTex.y )).r;

			/*i.uv_Diffuse += offsetMainTex;
			i.uv_DamDiffuse += offsetMainTex;
			i.uv_DamDiffuse2 += offsetMainTex;*/

			float4 col = tex2D(_Diffuse, i.uv_Diffuse);
			float4 dam = tex2D(_DamDiffuse, i.uv_DamDiffuse);
			float4 dam2 = tex2D(_DamDiffuse2, i.uv_DamDiffuse2);

			float4 bump = tex2D(_Bump, i.uv_Diffuse); bump.rg -= 0.5;
			float4 bumpd = tex2D(_BumpD, i.uv_DamDiffuse); bumpd.rg = (bumpd.rg - 0.5)*2;
			float4 bumpd2 = tex2D(_BumpD2, i.uv_DamDiffuse2); bumpd2.rg -= 0.5;


			float damAlpha = saturate((mask.r*3 - dam.a*2 + col.a - 1) * 8);
			float deAlpha = 1 - damAlpha;
			col = col*deAlpha + dam*damAlpha;
			bump = bump*deAlpha + bumpd*damAlpha;


			float damAlpha2 = ((mask.r ) - 0.5 - dam2.a + dam.a) * 4* damAlpha;

			
			damAlpha2 = saturate(damAlpha2);
			deAlpha = 1 - damAlpha2;
			col = col*deAlpha + dam2*damAlpha2;
			bump = bump*deAlpha + bumpd2*damAlpha2;

			o.Normal =normalize(float3((mask.r - maskg)*damAlpha2*8 + bump.r , -(mask.r - maskr)*damAlpha2*8  + bump.g , 0.1));

			o.Albedo = col.rgb;
			o.Smoothness = bump.b;
			o.Metallic = damAlpha2;
			o.Alpha = col.a;
			o.Occlusion = bump.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}