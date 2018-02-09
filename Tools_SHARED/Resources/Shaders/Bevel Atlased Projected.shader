Shader "Painter_Experimental/Bevel Atlased Projected" {
	Properties{
	[NoScaleOffset]_MainTex("Base texture Atlas", 2D) = "white" {}
	[NoScaleOffset]_BumpMapC("Combined Maps Atlas (RGB)", 2D) = "white" {}
	_Merge("_Merge", Range(0.01,25)) = 1
	[NoScaleOffset]_AtlasTextures("_Textures In Row _ Atlas", float) = 1
	}

		Category{
		Tags{
		"Queue" = "Geometry"
		"IgnoreProjector" = "True"
		"RenderType" = "Opaque"
		"LightMode" = "ForwardBase"
		"DisableBatching" = "True"
		"UVtype" = "Projected"
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
	sampler2D _BumpMapC;
	float4 _MainTex_TexelSize;
	float _AtlasTextures;

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
		float4 atlasedUV : TEXCOORD10;
		float4 bC : TEXCOORD11;
	};
	v2f vert(appdata_full v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);

		//o.texcoord = v.texcoord.xy;
		o.vcol = v.color;
		o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w); //v.texcoord1;
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

		float3 deEdge = 1 - o.edge.xyz;

		o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
		o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
		o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

		o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

		normalAndPositionToUV(o.snormal.xyz, o.worldPos, o.bC, o.texcoord.xy);

		TRANSFER_SHADOW(o);

		float atlasNumber = v.texcoord.z;

		float atY = floor(atlasNumber / _AtlasTextures);
		float atX = atlasNumber - atY*_AtlasTextures;
		float edge = _MainTex_TexelSize.x;

		o.atlasedUV.xy = float2(atX, atY) / _AtlasTextures;				//+edge;
		o.atlasedUV.z = edge;										//(1) / _AtlasTextures - edge * 2;
		o.atlasedUV.w = 1 / _AtlasTextures;



		return o;
	}



	float4 frag(v2f i) : SV_Target
	{


	float dist = length(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);
	float far = min(1, dist*0.01);
	float deFar = 1 - far;
	float mip = (0.5 *log2(dist));
	float seam = i.atlasedUV.z*pow(2, mip);
	float2 fractal = (frac(i.texcoord.xy)*(i.atlasedUV.w - seam) + seam*0.5);
	i.texcoord = fractal + i.atlasedUV.xy;


	float weight;

	float3 normal = DetectSmoothEdge(i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight); //(i.edge.xyz);
	float deWeight = 1 - weight;
	/*float2 perfTex = (floor(i.texcoord*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;
	float2 off = (i.texcoord - perfTex);
	off = off *saturate((abs(off) * _MainTex_TexelSize.z) * 40 - 19);
	perfTex += off;*/

	float4 col = tex2Dlod(_MainTex, float4(i.texcoord,0,mip));
	float4 bumpMap = tex2Dlod(_BumpMapC, float4(i.texcoord, 0, mip));
	bumpMap.rg -= 0.5;

	col = col*deWeight + i.vcol*weight;
	bumpMap = bumpMap*deWeight;
	bumpMap.b = bumpMap.b*deWeight + weight*i.vcol.a;
	bumpMap.a = bumpMap.a*deWeight + weight*0.5;

	float3 tnormal = float3(bumpMap.r, bumpMap.g, 1);

	applyTangentNonNormalized(i.bC, normal, bumpMap.rg);
	normal = normalize(normal);

	i.viewDir.xyz = normalize(i.viewDir.xyz);

	float dotprod = dot(i.viewDir.xyz, normal);					 //dot(normal,  i.viewDir.xyz);
	float fernel = 1.5 - dotprod;
	float ambientBlock = (1 - bumpMap.a)*dotprod;
	float shadow = saturate(SHADOW_ATTENUATION(i) * 2 - ambientBlock);
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*normal);
	float dott = max(0.01, dot(_WorldSpaceLightPos0, -reflected));

	float diff = saturate((dot(normal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	float direct = diff*shadow;

	col.rgb *= ((diff)*_LightColor0 //+ ShadeSH9(float4(normal, 1))
		
		)*(1 - bumpMap.b)*bumpMap.a;

	col.a += 0.01;

	float power = pow(col.a,8);

	col.rgb += (pow(dott, 4096 * power)*(_LightColor0.rgb 
		)* power
		 * 8 * direct +ShadeSH9(float4(-reflected, 1)))*bumpMap.b;

	return col;

	}
		ENDCG
	}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}