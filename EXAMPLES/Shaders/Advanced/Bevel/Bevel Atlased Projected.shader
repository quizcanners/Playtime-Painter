Shader "Playtime Painter/Bevel/Bevel Atlased Projected" {
	Properties{
	[NoScaleOffset]_MainTex_ATL("Base texture (ATL)", 2D) = "white" {}
	[KeywordEnum(None, Regular, Combined)] _BUMP ("Bump Map", Float) = 0
	[NoScaleOffset]_BumpMapC("Combined Maps Atlas (RGB)", 2D) = "gray" {}
	[Toggle(UV_PROJECTED)] _PROJECTED ("Projected UV", Float) = 0
	[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
	[NoScaleOffset]_AtlasTextures("_Textures In Row _ Atlas", float) = 1
	[Toggle(EDGE_WIDTH_FROM_COL_A)] _EDGE_WIDTH("Color A as Edge Width", Float) = 0
	[Toggle(CLIP_EDGES)] _CLIP("Clip Edges", Float) = 0
	[Toggle(UV_PIXELATED)] _PIXELATED("Smooth Pixelated", Float) = 0
	}
//https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html
	
	
SubShader {

		Tags{
		"Queue" = "Geometry"
		"IgnoreProjector" = "True"
		"RenderType" = "Opaque"
		"LightMode" = "ForwardBase"
		"DisableBatching" = "True"
		"Solution" = "Bevel"
	}

		Pass {


		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 3.0

#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

#pragma shader_feature  ___ UV_ATLASED
#pragma shader_feature  ___ UV_PROJECTED
#pragma shader_feature  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 

	sampler2D _MainTex_ATL;
	sampler2D _BumpMapC;
	float4 _MainTex_ATL_TexelSize;
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
		#if defined(UV_ATLASED)
			float4 atlasedUV : TEXCOORD10;
		#endif

		#if !_BUMP_NONE
			#if UV_PROJECTED
				float4 bC : TEXCOORD11;
			#else
				float4 wTangent : TEXCOORD11;
			#endif
		#endif
	};

	v2f vert(appdata_full v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);

		o.vcol = v.color;
		o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w); 
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

		float3 deEdge = 1 - o.edge.xyz;

		o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
		o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
		o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

		o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

		#if defined(UV_PROJECTED)
			normalAndPositionToUV(o.snormal.xyz, o.worldPos, 
			#if !_BUMP_NONE
				o.bC, 
			#endif
			o.texcoord.xy);
		#else

			#if !_BUMP_NONE
				o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
				o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
			#endif

			o.texcoord = v.texcoord.xy;

		#endif

		TRANSFER_SHADOW(o);

		#if defined(UV_ATLASED)
		vert_atlasedTexture(_AtlasTextures, v.texcoord.z, _MainTex_ATL_TexelSize.x, o.atlasedUV);
		#endif

		return o;
	}



	float4 frag(v2f i) : SV_Target {

	float mip = 0;

	#if defined(UV_ATLASED)
		float dist = length(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);
		#if	!UV_PIXELATED
			mip = (log2(dist));
		#endif

			frag_atlasedTexture(i.atlasedUV, mip, i.texcoord.xy);
	#endif


	#if UV_ATLASED 
		float4 col = tex2Dlod(_MainTex_ATL, float4(i.texcoord,0,mip));
	#else
		float4 col = tex2D(_MainTex_ATL, i.texcoord);
	#endif

	float weight;
	float3 normal = DetectSmoothEdge(
		1-col.a,
		i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight); 
	
	float deWeight = 1 - weight;

//#if CLIP_EDGES
	clip(dot(i.viewDir.xyz, normal));
//#endif

	col = col*deWeight + i.vcol*weight;

	#if !_BUMP_NONE


		#if UV_ATLASED //|| UV_PIXELATED
			float4 bumpMap = tex2Dlod(_BumpMapC, float4(i.texcoord, 0, mip));
		#else
			float4 bumpMap = tex2D(_BumpMapC, i.texcoord);
		#endif

		float3 tnormal;

		#if _BUMP_REGULAR
			tnormal = UnpackNormal(bumpMap);
			bumpMap = float4(0,0,0.5,1);
		#else
			bumpMap.rg = (bumpMap.rg - 0.5)*2;
			tnormal = float3(bumpMap.r, bumpMap.g, 1);
		#endif


		float3 preNorm = normal;

		#if UV_PROJECTED
			applyTangentNonNormalized(i.bC, normal, bumpMap.rg);
			normal = normalize(normal);
		#else
			applyTangent (normal, tnormal,  i.wTangent);
		#endif

		normal = normal*deWeight + preNorm*weight;

	#else
		float4 bumpMap = float4(0,0,0.5,1);
	#endif

	col.a = col.a*deWeight + weight*i.vcol.a;
	bumpMap.a = bumpMap.a*deWeight + weight*0.7;

	i.viewDir.xyz = normalize(i.viewDir.xyz);

	float dotprod = dot(i.viewDir.xyz, normal);					
	float fernel = 1.5 - dotprod;
	float ambientBlock = (1 - bumpMap.a)*dotprod;
	float shadow = saturate(SHADOW_ATTENUATION(i) * 2 - ambientBlock);
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*normal);
	float dott = max(0.01, dot(_WorldSpaceLightPos0, -reflected));

	float diff = saturate((dot(normal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * 4 * (1 - diff));
	float direct = diff*shadow;

	float3 ambientCol = ShadeSH9(float4(normal, 1));

	col.rgb *= (direct*_LightColor0.rgb*(1 - col.a) + ambientCol*bumpMap.a);
	

	float power = pow(col.a,8);

	col.rgb += (pow(dott, 4096 * power)*(_LightColor0.rgb )* power
		 * 8 * direct  +ShadeSH9(float4(-reflected, 1)))*col.a;

	BleedAndBrightness(col, fernel);


	return col;

	}
		ENDCG
		
	}

	

		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
			
	}
	FallBack "Diffuse"
	//CustomEditor "BevelMaterialInspector"

}