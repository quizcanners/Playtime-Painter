Shader "Playtime Painter/Geometry/Destructible/Walls" {
	Properties {
		[NoScaleOffset] _MainTex_ATL_UV2("_Main DAMAGE (_UV2) (_ATL) (RGB)", 2D) = "black" {}
		_WetColor("Wetness Color", Color) = (0.26,0.16,0.16,0.0)
		_Diffuse("ATL_Diffuse (_ATL)", 2D) = "white" {}
		[NoScaleOffset]_Bump("ATL_Bump (_ATL)", 2D) = "gray" {}
		[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		_AtlasTextures("_Textures In Row _ Atlas", float) = 1
		_DamDiffuse("Damaged Diffuse", 2D) = "white" {}
		[NoScaleOffset]_BumpD("Bump Damage", 2D) = "gray" {}
		_DamDiffuse2("Damaged Diffuse Deep", 2D) = "white" {}
		[NoScaleOffset]_BumpD2("Bump Damage 2", 2D) = "gray" {}
	}

	SubShader {

		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma vertex vert
		#pragma surface surf Standard fullforwardshadows
		#pragma multi_compile  ___ UV_ATLASED
		#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_cg.cginc"
		#pragma target 3.0

		sampler2D _MainTex_ATL_UV2;
		sampler2D _Diffuse;
		sampler2D _DamDiffuse;
		sampler2D _DamDiffuse2;
		sampler2D _Bump;
		sampler2D _BumpD;
		sampler2D _BumpD2;
		float4 _MainTex_ATL_UV2_TexelSize;
		float4 _WetColor;
		float _AtlasTextures;

		struct Input {
			float2 uv2_MainTex_ATL_UV2;
			float2 uv_Diffuse;
			float2 uv_DamDiffuse;
			float2 uv_DamDiffuse2;
			#if defined(UV_ATLASED)
			float4 atlasedUV : TEXCOORD6;
			#endif
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			#if defined(UV_ATLASED)
			vert_atlasedTexture(_AtlasTextures, v.texcoord.z, o.atlasedUV);
			#endif
		}

		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input i, inout SurfaceOutputStandard o) {

			#if UV_ATLASED

			i.uv2_MainTex_ATL_UV2.xy = (frac(i.uv2_MainTex_ATL_UV2.xy)*(i.atlasedUV.w) + i.atlasedUV.xy);
			
			float lod;

			atlasUVlod(i.uv_Diffuse, lod, _MainTex_ATL_UV2_TexelSize, i.atlasedUV);

			float4 col = tex2Dlod(_Diffuse, float4(i.uv_Diffuse, 0, lod)); // tex2Dlod(_Diffuse, float4(i.uv_Diffuse, 0, lod));
			float4 bump = tex2Dlod(_Bump, float4(i.uv_Diffuse, 0, lod)); 

			#else

			float4 col = tex2D(_Diffuse, i.uv_Diffuse);
			float4 bump = tex2D(_Bump, i.uv_Diffuse); 

			#endif

			float4 mask = tex2D(_MainTex_ATL_UV2, i.uv2_MainTex_ATL_UV2);

			//return mask;

			bump.rg -= 0.5;

			float maskr = tex2D(_MainTex_ATL_UV2, float2(i.uv2_MainTex_ATL_UV2.x,i.uv2_MainTex_ATL_UV2.y + _MainTex_ATL_UV2_TexelSize.y )).r;
			float maskg = tex2D(_MainTex_ATL_UV2, float2(i.uv2_MainTex_ATL_UV2.x + _MainTex_ATL_UV2_TexelSize.x , i.uv2_MainTex_ATL_UV2.y )).r;

			float4 dam = tex2D(_DamDiffuse, i.uv_DamDiffuse);
			float4 dam2 = tex2D(_DamDiffuse2, i.uv_DamDiffuse2);

			float4 bumpd = tex2D(_BumpD, i.uv_DamDiffuse); bumpd.rg -= 0.5;
			float4 bumpd2 = tex2D(_BumpD2, i.uv_DamDiffuse2); bumpd2.rg -= 0.5;

			float damAlpha = saturate((mask.r*3 + bump.b - bumpd.b*2  - 1) * 4);
			float deAlpha = 1 - damAlpha;
			col = col*deAlpha + dam*damAlpha;
			bump = bump*deAlpha + bumpd*damAlpha;

			float damAlpha2 = (((mask.r-0.5)*4 ) - 1 + (bumpd2.b - bumpd.b)*2) * 8* damAlpha;

			damAlpha2 = saturate(damAlpha2);
			deAlpha = 1 - damAlpha2;
			col = col*deAlpha + dam2*damAlpha2;
			bump = bump*deAlpha + bumpd2*damAlpha2;

			float water = saturate((mask.b*(2 + damAlpha + damAlpha2) - bump.b-1));

			o.Smoothness = max(col.a, water);

			water *= _WetColor.a;
			float deWater = 1 - water;

			o.Normal =normalize(float3((mask.r - maskg)*damAlpha2*4 + bump.r ,
				-(maskr - mask.r)*damAlpha2*4+ bump.g, 0.1)*deWater +float3(0,0,1)*water);

			o.Albedo = col.rgb;// *deWater + _WetColor.rgb*water;

			o.Metallic = water;
			o.Alpha = col.a;
			o.Occlusion = bump.a*deWater+water;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
