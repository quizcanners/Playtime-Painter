Shader "Playtime Painter/Baked Shadows/In Texture" {
	Properties{
		[NoScaleOffset] _MainTex("_MainTex  (_ATL) (RGB)", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map (_ATL)", Float) = 0
		[NoScaleOffset]_BumpMapC("Combined Maps Atlas (RGB)", 2D) = "gray" {}
		[NoScaleOffset] _BakedShadows_UV2("_BakedShadows (_UV2)", 2D) = "white" {}
		[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		[NoScaleOffset]_AtlasTextures("_Textures In Row _ Atlas", float) = 1

		l0pos("Point light 0 world scene position", Vector) = (0,0,0,0)
		l0col("Point light 0 Color", Vector) = (0,0,0,0)
		l1pos("Point light 1 world scene position", Vector) = (0,0,0,0)
		l1col("Point light 1 Color", Vector) = (0,0,0,0)
		l2pos("Point light 2 world scene position", Vector) = (0,0,0,0)
		l2col("Point light 2 Color", Vector) = (0,0,0,0)
		
	}
		Category{
		Tags{ "Queue" = "Geometry"
		"IgnoreProjector" = "True"
		"RenderType" = "Opaque"
		"LightMode" = "ForwardBase"
	}


		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdbase
#pragma target 3.0
#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

#pragma shader_feature  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 
#pragma shader_feature  ___ UV_ATLASED

	uniform sampler2D _MainTex;
	uniform sampler2D _BumpMapC;
	uniform sampler2D _BakedShadows_UV2;


	float4 l0pos;
	float4 l0col;
	float4 l1pos;
	float4 l1col;
	float4 l2pos;
	float4 l2col;
	float4 _MainTex_TexelSize;
	float _AtlasTextures;

	struct v2f {
		float4 pos : SV_POSITION;
		float4 vcol : COLOR0;
		float3 worldPos : TEXCOORD0;
		float3 normal : TEXCOORD1;
		float2 texcoord : TEXCOORD2;
		float2 texcoord2 : TEXCOORD3;
		SHADOW_COORDS(4)
		float3 viewDir: TEXCOORD5;
#if defined(UV_ATLASED)
		float4 atlasedUV : TEXCOORD6;
#endif
#if !_BUMP_NONE
		float4 wTangent : TEXCOORD7;
#endif
	};


	v2f vert(appdata_full v) {
		v2f o;

	
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);

		o.vcol = v.color;
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

	#if !_BUMP_NONE
		o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
		o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
	#endif

		o.texcoord = v.texcoord.xy;
		o.texcoord2 = v.texcoord2.xy;

		TRANSFER_SHADOW(o);

#if defined(UV_ATLASED)
		vert_atlasedTexture(_AtlasTextures, v.texcoord.z, _MainTex_ATL_TexelSize.x, o.atlasedUV);
#endif
		
		return o;
	}



	float4 frag(v2f i) : COLOR{


#if UV_ATLASED

	i.texcoord2.xy = (frac(i.texcoord2.xy)*(i.atlasedUV.w) + i.atlasedUV.xy);

	float lod;

	atlasUVlod(i.texcoord, lod, _MainTex_TexelSize, i.atlasedUV);

	float4 col = tex2Dlod(_MainTex, float4(i.texcoord, 0, lod)); 
	
#if !_BUMP_NONE
	float4 bumpMap = tex2Dlod(_BumpMapC, float4(i.texcoord, 0, lod));
#endif

#else

	float4 col = tex2D(_MainTex, i.texcoord);
#if !_BUMP_NONE
	float4 bumpMap = tex2D(_BumpMapC, i.texcoord);
#endif

#endif



#if !_BUMP_NONE
	float3 tnormal;

#if _BUMP_REGULAR
	tnormal = UnpackNormal(bumpMap);
	bumpMap = float4(0,0,0.5,1);
#else
	bumpMap.rg = (bumpMap.rg - 0.5) * 2;
	tnormal = float3(bumpMap.r, bumpMap.g, 1);
#endif

	applyTangent(i.normal, tnormal,  i.wTangent);

#else
	float4 bumpMap = float4(0,0,0.5,1);
#endif

	i.viewDir.xyz = normalize(i.viewDir.xyz);



	float dotprod = dot(i.viewDir.xyz, i.normal);
	float fernel = 1.5 - dotprod;
	float ambientBlock = (1 - bumpMap.a)*dotprod;
	float shadow = saturate(SHADOW_ATTENUATION(i) * 2 - ambientBlock);
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal);
	ambientBlock *= 4;

	// Point Lights:

	float4 bake = tex2Dlod(_BakedShadows_UV2, float4(i.texcoord2, 0, 0));
	float4 directBake = saturate((bake - 0.9) * 10);

	const float fourPi = 4 * 3.14;
	float power = pow(col.a, 8) * 4096;

	float3 scatter = 0;
	float3 glossLight = 0;
	float3 directLight = 0;

	//0 - Point Light
	float3 vec = i.worldPos.xyz - l0pos.xyz;
	float len = length(vec);
	vec /= len;

	float direct = max(0,dot(i.normal.xyz,-vec));
	direct = saturate(direct - ambientBlock * (1 - direct))*directBake.r; // Multiply by shadow

	float3 distApprox = l0col.rgb / (fourPi*(len*len));

	directLight += distApprox * direct;
	scatter += distApprox * bake.r * l0col.a;


	float3 halfDirection = normalize(i.viewDir.xyz - vec);
	float NdotH = max(0.01, (dot(i.normal, halfDirection)));
	float normTerm = pow(NdotH, power)*power*0.01;

	glossLight += normTerm*l0col.rgb*direct;

	//1 - Point Light
	 vec = i.worldPos.xyz - l1pos.xyz;
	 len = length(vec);
	vec /= len;

	 direct = max(0, dot(i.normal.xyz, -vec));
	direct = saturate(direct - ambientBlock * (1 - direct))*directBake.g; // Multiply by shadow

	 distApprox = l1col.rgb / (fourPi*(len*len));

	directLight += distApprox * direct;
	scatter += distApprox * bake.g * l1col.a;


	 halfDirection = normalize(i.viewDir.xyz - vec);
	 NdotH = max(0.01, (dot(i.normal, halfDirection)));
	 normTerm = pow(NdotH, power)*power*0.01;

	glossLight += normTerm * l1col.rgb*direct; 

	//2 - Point Light
	vec = i.worldPos.xyz - l2pos.xyz;
	len = length(vec);
	vec /= len;

	direct = max(0, dot(i.normal.xyz, -vec));
	direct = saturate(direct - ambientBlock * (1 - direct))*directBake.b; // Multiply by shadow

	distApprox = l2col.rgb / (fourPi*(len*len));

	directLight += distApprox * direct;
	scatter += distApprox * bake.b * l2col.a;


	halfDirection = normalize(i.viewDir.xyz - vec);
	NdotH = max(0.01, (dot(i.normal, halfDirection)));
	normTerm = pow(NdotH, power)*power*0.01;

	glossLight += normTerm * l2col.rgb*direct;


	// Baked Shadows End


	float dott = max(0.01, dot(_WorldSpaceLightPos0, -reflected));
	float diff = saturate((dot(i.normal, _WorldSpaceLightPos0.xyz)));
	diff = saturate(diff - ambientBlock * (1 - diff));
	direct = diff * shadow;


	halfDirection = normalize(i.viewDir.xyz + _WorldSpaceLightPos0.xyz);
	NdotH = max(0.01, (dot(i.normal, halfDirection)));
	normTerm = pow(NdotH, power)*power*0.01;

	col.rgb *= (direct*_LightColor0.rgb + directLight + (ShadeSH9(float4(i.normal, 1))*bake.a + scatter) * bumpMap.a);

	col.rgb += (normTerm *_LightColor0.rgb * direct + glossLight + ShadeSH9(float4(-reflected, 1))*bake.a)*col.a;

	BleedAndBrightness(col, 1);

	return col;

	}
		ENDCG
			
	}
	UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	FallBack "Diffuse"
	}
	
}
