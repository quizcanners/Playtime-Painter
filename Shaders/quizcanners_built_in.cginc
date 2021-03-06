#include "quizcanners_cg.cginc"
#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "AutoLight.cginc"



inline float3 GetParallax(float4 tangent, float3 normal, float4 vertex) {

	float3x3 objectToTangent = float3x3(
		tangent.xyz,
		cross(normal, tangent.xyz) * tangent.w,
		normal
		);

	float3 tangentViewDir = mul(objectToTangent, ObjSpaceViewDir(vertex));

	return tangentViewDir;
}


inline void Combined_Light(inout float4 col, float ambient, float smoothness, float3 worldNormal, float3 viewDir, float shadow) {

	float dotprod = max(0, dot(worldNormal, viewDir.xyz));

	float fernel = (1.5 - dotprod)*0.66;
	float3 reflected = normalize(viewDir.xyz - 2 * (dotprod)*worldNormal);// *fernel

	float deSmoothness = (1 - smoothness);

	float ambientBlock = (1 - ambient)*dotprod; // MODIFIED

	shadow = saturate(shadow * 2 - ambientBlock);

	float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz))); // _WorldSpaceLightPos0 will not look right in the editor
	diff = saturate(diff - ambientBlock * 8 * (1 - diff));
	float direct = diff*shadow;

	float3 ambientRefl = ShadeSH9(float4(normalize(-reflected), 1));
	float3 ambientCol = ShadeSH9(float4(worldNormal, 1));

	_LightColor0 *= direct;

	col.rgb *= (_LightColor0  + ambientCol* fernel) * (deSmoothness);
	
	float3 halfDirection = normalize(viewDir.xyz + _WorldSpaceLightPos0.xyz);

	float NdotH = max(0.01, (dot(worldNormal, halfDirection)));
	
	float power = smoothness * 8; // pow(smoothness, 8) * 1024;

	float normTerm =pow(NdotH, power)*power*0.1;

	float3 reflResult = 
		(normTerm *_LightColor0 + ambientRefl.rgb)
		* smoothstep(0.75, 1, 1- ambientBlock)
		* smoothness * fernel;
	
	col.rgb += reflResult;

}



inline void Simple_Light(float height,float3 worldNormal, float3 viewDir, inout float4 col, float shadow, float reflectivness) {

	float dotprod = max(0, dot(worldNormal, viewDir.xyz));
	float fernel = 1.5 - dotprod;
	float3 reflected = normalize(viewDir.xyz - 2 * (dotprod)*worldNormal);



	float ambientBlock = (1 - height)*dotprod; // MODIFIED

	shadow = saturate((shadow * 2 - ambientBlock));

	float diff = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	float direct = diff*shadow;


	float3 ambientRefl = ShadeSH9(float4(reflected, 1));
	float3 ambientCol = ShadeSH9(float4(worldNormal, 1));

	_LightColor0 *= direct;

	col.rgb = col.rgb* (_LightColor0 +
		(ambientCol
			));

	float3 halfDirection = normalize(viewDir.xyz + _WorldSpaceLightPos0.xyz);

	float NdotH = max(0.01, (dot(worldNormal, halfDirection)));// *pow(smoothness + 0.2, 8);

	//float power = pow(smoothness, 8) * 1024;

	//float normTerm = pow(NdotH, power)*power*0.01;

		float smoothness = reflectivness;
	float deSmoothness = (1 - smoothness);

	float3 reflResult = (
		NdotH * _LightColor0 * reflectivness +

		ambientRefl.rgb

		)* fernel;

	col.rgb += reflResult;


}

inline void DirectionalLightTransparent(inout float3 scatter, inout float3 directLight,
	float shadow, float3 normal, float3 viewDir, float ambientBlock, float bake) {
	
	_LightColor0.rgb *= shadow;

	float dott =  dot(viewDir, -_WorldSpaceLightPos0.xyz);

	float power = pow(max(0.01, dott), 256);

	scatter += ShadeSH9(float4(normal, 1))*bake +power * _LightColor0.rgb*8;
	directLight += _LightColor0.rgb*max(0.1,-dott);

}

inline void DirectionalLight(inout float3 scatter, inout float3 glossLight, inout float3 directLight, 
	float shadow,  float3 normal, float3 viewDir, float ambientBlock, float bake, float power) {
	shadow = saturate(shadow * 2 - ambientBlock);

	float direct = max(0, dot(_WorldSpaceLightPos0, normal));
	
	direct = direct * shadow; // Multiply by shadow

	float3 halfDirection = normalize(viewDir.xyz + _WorldSpaceLightPos0.xyz);
	float NdotH = max(0.01, (dot(normal, halfDirection)));
	float normTerm = pow(NdotH, power);

	_LightColor0.rgb *= direct;

	scatter += ShadeSH9(float4(normal, 1))*bake;
	glossLight += normTerm *_LightColor0.rgb*power*0.1;
	directLight += _LightColor0.rgb;
	

}