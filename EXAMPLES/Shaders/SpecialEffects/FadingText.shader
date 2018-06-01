Shader "Painter_Experimental/FadingText" {
	Properties
	{
		_MainTex("Font Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_FadeIn("Fade In Parameter", Range(0,1)) = 0
		_FadeWidth("Fade Width", Range(0,1)) = 0
		_offset("_offset", float) = 0

	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100
		Cull Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
		// make fog work
#pragma multi_compile_fog

#include "UnityCG.cginc"

		struct appdata_t
	{
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 uv : TEXCOORD0;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _MainTex_TexelSize;
	uniform fixed4 _Color;
	float _FadeIn;
	float _FadeWidth;
	float _offset;

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float4 color : COLOR;
		float2 uv : TEXCOORD0;
		float4 screenPos : TEXCOORD1;
		float fadeMask : TEXCOORD2;
	};

	v2f vert(appdata_full v)
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	
		o.vertex = UnityObjectToClipPos(v.vertex);

		o.screenPos = ComputeScreenPos(o.vertex);

		float2 sp = o.screenPos.xy / o.screenPos.w;
		_FadeWidth = 1 / _FadeWidth;
		o.fadeMask = saturate((_FadeIn - sp.x) *  _FadeWidth);

		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		//float3 worldNormal = UnityObjectToWorldNormal(v.normal);//+float3(-0.1, -0.1, 0);

		float2 offCenter = sp - 0.5;

		v.vertex += mul(unity_WorldToObject, pow((1-o.fadeMask),4)*float3(offCenter.x * _offset, offCenter.y*_offset, 0));

		o.vertex = UnityObjectToClipPos(v.vertex);

	
		o.uv = TRANSFORM_TEX(v.texcoord, _MainTex); 

		o.color = v.color * _Color;

		return o;
	}


	float4 frag(v2f i) : SV_Target{

		
		_Color.rgb += (1 - _Color.rgb)*saturate((_FadeIn - 0.5) * 2);

	_MainTex_TexelSize.xy *= (1 - i.fadeMask);

		_Color.a = tex2D(_MainTex, i.uv).a*(
			tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x,0)).a
			+ tex2D(_MainTex, i.uv + float2(0,-_MainTex_TexelSize.y)).a
			+ tex2D(_MainTex, i.uv + float2(-_MainTex_TexelSize.x, 0)).a
			+ tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y)).a
			)*0.25;

		_Color.a *= i.fadeMask;

		_Color.rgb += (1-i.fadeMask);

		return _Color;
	}
		ENDCG
	}
	}
}
