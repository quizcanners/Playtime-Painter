Shader "Editor/br_TerrainPreview" {
	Properties{
	//	[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
	[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
	[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
	[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
	[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}
	[HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
	[HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
	[HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
	[HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}

	[HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "white" {}
	[HideInInspector] _Color("Main Color", Color) = (1,1,1,1)
	}

		CGINCLUDE
#pragma surface surf Lambert vertex:SplatmapVertPreview finalcolor:SplatmapFinalColor finalprepass:SplatmapFinalPrepass finalgbuffer:SplatmapFinalGBuffer noinstancing
#include "TerrainSplatmapCommon.cginc"
#include "qc_Includes.cginc"

		sampler2D _mergeControl;
		sampler2D _mergeTerrainHeight;
		float4 _mergeTeraPosition;
		float4 _mergeTerrainScale;

		void SplatmapVertPreview(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.tc_Control = TRANSFORM_TEX(v.texcoord, _Control); 


			float4 height = tex2Dlod(_mergeTerrainHeight, float4(data.tc_Control.xy,0,0));

			float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
			worldPos.y = _mergeTeraPosition.y + height.a*_mergeTerrainScale.y;
			v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz, v.vertex.w));

			float4 pos = UnityObjectToClipPos(v.vertex);

			//UNITY_TRANSFER_FOG(data, pos);

#ifdef _TERRAIN_NORMAL_MAP
			v.tangent.xyz = cross(v.normal, float3(0, 0, 1));
			v.tangent.w = -1;
#endif
		}


		void MyPreviewMix(Input IN, out half4 splat_control, out fixed4 mixedDiffuse) {
		splat_control = tex2D(_mergeControl, IN.tc_Control);
		mixedDiffuse = 0.0f;

#ifdef TERRAIN_STANDARD_SHADER
		mixedDiffuse += splat_control.r * tex2D(_Splat0, IN.uv_Splat0) * half4(1.0, 1.0, 1.0, defaultAlpha.r);
		mixedDiffuse += splat_control.g * tex2D(_Splat1, IN.uv_Splat1) * half4(1.0, 1.0, 1.0, defaultAlpha.g);
		mixedDiffuse += splat_control.b * tex2D(_Splat2, IN.uv_Splat2) * half4(1.0, 1.0, 1.0, defaultAlpha.b);
		mixedDiffuse += splat_control.a * tex2D(_Splat3, IN.uv_Splat3) * half4(1.0, 1.0, 1.0, defaultAlpha.a);
#else
		mixedDiffuse += splat_control.r * tex2D(_Splat0, IN.uv_Splat0);
		mixedDiffuse += splat_control.g * tex2D(_Splat1, IN.uv_Splat1);
		mixedDiffuse += splat_control.b * tex2D(_Splat2, IN.uv_Splat2);
		mixedDiffuse += splat_control.a * tex2D(_Splat3, IN.uv_Splat3);
#endif
		
	}


	void surf(Input IN, inout SurfaceOutput o) {
		half4 splat_control;
		fixed4 mixedDiffuse;
		MyPreviewMix(IN, splat_control,  mixedDiffuse);
		o.Albedo = mixedDiffuse.rgb;
		o.Alpha = 1;
	}
	ENDCG

		Category{
		Tags{
		"Queue" = "Geometry-99"
		"RenderType" = "Opaque"
	}
		// TODO: Seems like "#pragma target 3.0 _TERRAIN_NORMAL_MAP" can't fallback correctly on less capable devices?
		// Use two sub-shaders to simulate different features for different targets and still fallback correctly.
		SubShader{ // for sm3.0+ targets
		CGPROGRAM
#pragma target 3.0
#pragma multi_compile __ _TERRAIN_NORMAL_MAP
		ENDCG
	}
		SubShader{ // for sm2.0 targets
		CGPROGRAM
		ENDCG
	}
	}

		//Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass"
		//Dependency "BaseMapShader" = "Diffuse"
		//Dependency "Details0" = "Hidden/TerrainEngine/Details/Vertexlit"
		//Dependency "Details1" = "Hidden/TerrainEngine/Details/WavingDoublePass"
		//Dependency "Details2" = "Hidden/TerrainEngine/Details/BillboardWavingDoublePass"
		//1Dependency "Tree0" = "Hidden/TerrainEngine/BillboardTree"

		Fallback "Diffuse"
}
