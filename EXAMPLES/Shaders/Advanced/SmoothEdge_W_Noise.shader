Shader "Playtime Painter/Walls/Noise Experiment" {
	Properties{
		_MainTex("Base texture", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
	}

		Category{
		Tags{ "LightMode" = "ForwardBase"
		"Solution" = "AtlasedProjected"
	}




		SubShader
	{
		Pass
	{


		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	sampler2D _Noise;

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

	};
	v2f vert(appdata_full v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);
		o.snormal.xyz = UnityObjectToWorldNormal(v.tangent.xyz);
		o.texcoord = v.texcoord.xy;
		o.vcol = v.color;
		o.edge = v.texcoord3;
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

		TRANSFER_SHADOW(o);

		return o;
	}



	float4 frag(v2f i) : SV_Target
	{

	float2 border = DetectEdge(i.edge);
	float deBorder = 1 - border.x;
	float deEdge = 1 - border.y;
	i.normal = i.snormal*deBorder + i.normal*border.x;


	//float3 worldViewDir = i.wnormal;
	//float3 worldRefl = reflect(-worldViewDir, worldNormal);

	float4 noise = tex2Dlod(_Noise, float4(frac(i.viewDir.x + i.worldPos.y + _Time.x) * 1128, frac(i.viewDir.y + i.worldPos.z - _Time.x) * 1128, 0, 0));//(frac((i.viewDir.x+i.worldPos.y) * 11123 + (i.viewDir.y+i.worldPos.z) * 11456 + (i.viewDir.z+i.worldPos.x) * 12789 * _Time.x) - 0.5);


	float2 perfTex = (floor(i.texcoord*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;
	float2 off = (i.texcoord - perfTex);
	off = off *saturate((abs(off) * _MainTex_TexelSize.z) * 40 - 19);
	perfTex += off ;

	float4 col = tex2Dlod(_MainTex, float4(perfTex,0,0));
	col = col*deEdge + i.vcol*border.y;
	float shadow = SHADOW_ATTENUATION(i);
	
	i.viewDir.xyz = normalize(i.viewDir.xyz);

	float dotprod = dot(i.viewDir.xyz, i.normal.xyz);					 //dot(normal,  i.viewDir.xyz);
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal.xyz);
	float dott = max(0.01, dot(_WorldSpaceLightPos0, -reflected));

	col.rgb *= (max(0, dot(i.normal, _WorldSpaceLightPos0.xyz))
			* shadow + 1)*_LightColor0*0.5*(1- col.a);

	col.a += 0.01;

	float power = pow(col.a,8);

	col.rgb += pow(dott, 4096* power)*(_LightColor0.rgb +noise
		)* power
		*col.a*8;

	return col;
	}
		ENDCG
	}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}