Shader "Painter_Experimental/Surface_Atlased" {
	Properties {
		_MainTex("Base texture (ATL)", 2D) = "white" {}
		_BumpMap("Normal Map (ATL)", 2D) = "bump" {}
		_Metallic("Metallic ", float ) = 0.5
		_Glossiness("Glossiness ", float) = 0.5
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
		#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"
		#pragma multi_compile  ___ UV_ATLASED
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _OcclusionMap;
		float _Metallic;
		float _Glossiness;
		float _AtlasTextures;
		float4 _MainTex_TexelSize;

		struct Input {
			float2 uv_MainTex;
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
			atlasUVlod(i.uv_MainTex.xy, mip, _MainTex_TexelSize, i.atlasedUV);
			float4 uvMip = float4 (i.uv_MainTex, 0, mip);
			float4 col = tex2Dlod(_MainTex, uvMip);
			o.Normal = UnpackNormal(tex2Dlod(_BumpMap, uvMip));
			o.Occlusion = tex2Dlod(_OcclusionMap, uvMip).a;
#else
			float4 col = tex2D(_MainTex, i.uv_MainTex);
			o.Normal = UnpackNormal(tex2D(_BumpMap, i.uv_MainTex));
			o.Occlusion = tex2D(_OcclusionMap, i.uv_MainTex).a;
#endif

			
			o.Albedo = col.rgb;
			
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = col.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
