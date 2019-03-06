Shader "Playtime Painter/Geometry/Walls/Graffity" {
	Properties{
		_MainTex("Diffuse", 2D) = "white" {}
		[NoScaleOffset] _Overlay("Graffity (Texcoord 2, Transparent Layer)", 2D) = "black" {}
		[Toggle(_Metal)] metal("Metal", Float) = 0
		_Bump("Bump", 2D) = "bump" {}
	}

	SubShader{
		Tags{ 
			"RenderType" = "Opaque"
			"TextureSampledWithUV2_Overlay" = "true"
			"TransparentLayerExpected_Overlay" = "true"
		}

		LOD 200

		CGPROGRAM
		#pragma vertex vert
		#pragma surface surf Standard fullforwardshadows
		#pragma multi_compile _____ _Metal
		#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Overlay;
		sampler2D _Bump;

		struct Input {
			float2 uv2_Overlay;
			float2 uv_MainTex;
			float2 uv_Bump;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
		}


		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input i, inout SurfaceOutputStandard o) {

			float4 col = tex2D(_MainTex, i.uv_MainTex);
		
			float4 overlay = tex2D(_Overlay, i.uv2_Overlay);
			float deOverlay = 1 - overlay.a;

			o.Smoothness = 0.6*col.r; //overlay.a * 0.5 + col.a*deOverlay;

			#if _Metal
			o.Metallic = deOverlay;
			#endif

			float4 bump = tex2D(_Bump, i.uv_Bump);
			bump.rg -= 0.5;
			o.Normal = normalize(float3(bump.r , bump.g, 0.75 + overlay.a*0.25));

			o.Albedo = col.rgb * deOverlay + overlay.rgb*overlay.a;



			//col.rg = i.uv2_Overlay.xy;
			//col.ba = 1;
			//o.Albedo = col.rgb;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
