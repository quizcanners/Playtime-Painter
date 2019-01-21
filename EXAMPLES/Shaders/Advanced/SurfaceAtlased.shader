Shader "Playtime Painter/Walls/Surface Atlased" {
	Properties {
		[NoScaleOffset]_MainTex_ATL("Base texture (_ATL)", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map", Float) = 0
		[NoScaleOffset] _CombinedMap("Normal Map (ATL)", 2D) = "gray" {}
		//_Glossiness("Glossiness ", Range(0.1,0.9)) = 0.5
		_OcclusionMap("Occlusion (ATL)", 2D) = "white" {}
		[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		_AtlasTextures("_Textures In Row _ Atlas", float) = 1
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma vertex vert
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows
		#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"
		#pragma multi_compile  ___ UV_ATLASED
		#pragma multi_compile  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 

		#pragma target 3.0

		sampler2D _MainTex_ATL;
		sampler2D _CombinedMap;
		sampler2D _OcclusionMap;
	//	float _Glossiness;
		float _AtlasTextures;
		float4 _MainTex_ATL_TexelSize;

		struct Input {
			float2 uv_MainTex_ATL;
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

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input i, inout SurfaceOutputStandard o) {

			#if UV_ATLASED
			float mip = 0;
			atlasUVlod(i.uv_MainTex_ATL.xy, mip, _MainTex_ATL_TexelSize, i.atlasedUV);
			float4 uvMip = float4 (i.uv_MainTex_ATL, 0, mip);
			float4 col = tex2Dlod(_MainTex_ATL, uvMip);
			//col.a += (1 - col.a)*0.5;
			//col.a -= (col.a)*mip / 16;

			#if !_BUMP_NONE
			float4 cmap = tex2Dlod(_CombinedMap, uvMip);
			#endif
		
			#if _BUMP_REGULAR
			o.Occlusion = tex2Dlod(_OcclusionMap, uvMip).a;
			#endif

			#else
			float4 col = tex2D(_MainTex_ATL, i.uv_MainTex_ATL);

			#if !_BUMP_NONE
			float4 cmap = tex2D(_CombinedMap, i.uv_MainTex_ATL);
			#endif

			#if _BUMP_REGULAR
			o.Occlusion = tex2D(_OcclusionMap, i.uv_MainTex_ATL).a;
			#endif

			#endif

			#if _BUMP_REGULAR
			o.Normal = UnpackNormal(cmap);	
			#endif

			#if _BUMP_COMBINED
			cmap.rg = (cmap.rg - 0.5) * 2;
			o.Normal = float3(cmap.r, cmap.g, 1);
			#endif

			o.Albedo = col.rgb;

			#if _BUMP_COMBINED
			o.Occlusion = cmap.a;		
			#endif
			o.Smoothness = col.a;

		}
		ENDCG
	}
	FallBack "Diffuse"
}
