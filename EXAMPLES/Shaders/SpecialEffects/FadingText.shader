Shader "PlaytimePainter/Basic/FadingText" {
	Properties
	{
		[PerRendererData]_MainTex("Font Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_FadeIn("Fade In-Out", Range(0,2)) = 0
		_offset("_offset", float) = 0

		[Toggle] _ISUI("Is UIElement", Float) = 0
	}
		SubShader
	{
		Tags{ "RenderType" = "Transparent"
		"LightMode" = "ForwardBase"
		"Queue" = "Overlay+10"
	}
		LOD 100
		Cull Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile ___  _ISUI_ON
#include "UnityCG.cginc"

		struct appdata_t
	{
		float4 vertex : POSITION;
		float4 color : COLOR;
		float2 uv : TEXCOORD0;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float4 _MainTex_TexelSize;
	float4 _Color;
	float _FadeIn;
	float _offset;
	float4 _ClickLight;

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float4 color : COLOR;
		float2 uv : TEXCOORD0;
		float4 screenPos : TEXCOORD1;
		float2 fadeMask : TEXCOORD2;
		float3 wpos : TEXCOORD3;
	};

	v2f vert(appdata_full v)
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


		o.vertex = UnityObjectToClipPos(v.vertex);
		float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.wpos = worldPos;
		o.screenPos = ComputeScreenPos(o.vertex);

		o.wpos.z += 5;
		o.wpos.xy *= 4.35;

		const float _FadeWidth = 4;

#if _ISUI_ON
		_FadeIn = 2 - v.color.a;
#endif

		float2 sp = o.screenPos.xy / o.screenPos.w;
		o.fadeMask.x = saturate((_FadeIn - sp.x) *  _FadeWidth);
		float fadeOut = saturate((_FadeIn - 1));

		o.fadeMask.y = saturate((sp.x - fadeOut) *  _FadeWidth);

		o.fadeMask = saturate(o.fadeMask + saturate(1 - abs(_FadeIn - 1) * 4));

	
		float2 offCenter = sp - 0.5;

		v.vertex += mul(unity_WorldToObject, pow((1 - o.fadeMask.x),4)*float3(offCenter.x * _offset, offCenter.y*_offset, 0) -
			pow((1 - o.fadeMask.y), 4)*float3(offCenter.x * _offset, offCenter.y*_offset, 0)
		);

		o.vertex = UnityObjectToClipPos(v.vertex);


		o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

		o.color = v.color*_Color;

		return o;
	}


	float4 frag(v2f i) : SV_Target{

		float3 lightPos = _ClickLight.xyz - i.wpos.xyz;
		float dist = length(lightPos.xy);
		float _Blur = max((1 - dist)*_ClickLight.w,0);

#if _ISUI_ON
		i.color.a = saturate(i.color.a * 4);
#endif

		_Color = i.color;

		_Color.rgb += (1 - _Color.rgb)*saturate((_FadeIn - 0.5) * 2);

		_MainTex_TexelSize.xy *= (1 - i.fadeMask + _Blur * 4);

		_Color.a *= tex2D(_MainTex, i.uv).a;
		_Color.a *= i.fadeMask.x * (i.fadeMask.y);

		_Color.rgb += (1 - i.fadeMask.x) + _Blur;

		return _Color;
	}
		ENDCG
	}
	}
}
