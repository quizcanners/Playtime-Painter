Shader "Bevel/Bumped" {
	Properties{
	_MainTex("Base texture", 2D) = "white" {}
	_BumpMap("Normal Map", 2D) = "bump" {}
	//_Cube("Environment Map", Cube) = "" {}
	//_Noise("Noise", 2D) = "white" {}
	}

		Category{
		Tags{
		"Queue" = "Geometry"
		"IgnoreProjector" = "True"
		"RenderType" = "Opaque"
		"LightMode" = "ForwardBase"
		"DisableBatching" = "True"
		"UVtype" = "Normal"
		"Solution" = "Bevel"
	}




		SubShader
	{
		Pass
	{


		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "VertexDataProcessInclude.cginc"

#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight


#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED

		sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	sampler2D _BumpMap;
	//sampler2D _Noise;
	//uniform samplerCUBE _Cube;

	struct v2f {
		float4 pos : SV_POSITION;
		float4 vcol : COLOR0;
		float3 worldPos : TEXCOORD0;
		float3 normal : TEXCOORD1;
		float2 texcoord : TEXCOORD2;
		float4 edge : TEXCOORD3;
		float3 snormal: TEXCOORD4;
		SHADOW_COORDS(5)
		float3 viewDir: TEXCOORD6;
		float3 edgeNorm0 : TEXCOORD7;
		float3 edgeNorm1 : TEXCOORD8;
		float3 edgeNorm2 : TEXCOORD9;
		float4 wTangent : TEXCOORD10;

	};
	v2f vert(appdata_full v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);


		o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
		o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;

		o.texcoord = v.texcoord.xy;
		o.vcol = v.color;
		o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w); //v.texcoord1;
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

		float3 deEdge = 1 - o.edge.xyz;

		o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
		o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
		o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

		o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

		TRANSFER_SHADOW(o);

		return o;
	}



	float4 frag(v2f i) : SV_Target
	{

		float weight;

	

/*	float2 perfTex = (floor(i.texcoord*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;
	float2 off = (i.texcoord - perfTex);
	off = off *saturate((abs(off) * _MainTex_TexelSize.z) * 40 - 19);
	perfTex += off;*/


	float4 col = tex2D(_MainTex, i.texcoord);
	//float4 noise = tex2Dlod(_Noise, float4(frac(i.viewDir.x + i.worldPos.y + _Time.x) * 1128, frac(i.viewDir.y + i.worldPos.z - _Time.x) * 1128, 0, 0));//(frac((i.viewDir.x+i.worldPos.y) * 11123 + (i.viewDir.y+i.worldPos.z) * 11456 + (i.viewDir.z+i.worldPos.x) * 12789 * _Time.x) - 0.5);
	float3 tnormal = UnpackNormal(tex2D(_BumpMap, i.texcoord));

	float3 normal = DetectSmoothEdge(i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight); //(i.edge.xyz);

	//col.rgb = normal;
	//return i.wTangent;

	float deWeight = 1 - weight;
	col = col*deWeight + i.vcol*weight;
	//tnormal = tnormal*deWeight + float3(0, 1, 0)*weight;

	float3 preNorm = normal;

	applyTangent (normal, tnormal,  i.wTangent);
	
	normal = normal*deWeight + preNorm*weight;

	
	float shadow = SHADOW_ATTENUATION(i);

	i.viewDir.xyz = normalize(i.viewDir.xyz);

	float dotprod = dot(i.viewDir.xyz, normal);					 //dot(normal,  i.viewDir.xyz);
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*normal);
	float dott = max(0.01, dot(_WorldSpaceLightPos0, -reflected));

	col.rgb *= ((max(0, dot(normal, _WorldSpaceLightPos0.xyz))
		* shadow)*_LightColor0 //+ sky.rgb
		)*(1 - col.a);

	col.a += 0.01;

	float power = pow(col.a,8 );

	col.rgb += (pow(dott, 4096 * power)*(_LightColor0.rgb 
		)* power * 8 * shadow //+ sky.rgb
		)
		*col.a ;


	return 
	col;

	}
		ENDCG
	}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}