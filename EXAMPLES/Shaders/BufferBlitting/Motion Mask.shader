Shader "Playtime Painter/Buffer Blit/Motion Mask" {
	Properties{
		_MainTex("Camera Input", 2D) = "white" {}
		_Previous("Previous Frame", 2D) = "white" {}
	}
		Category{
		Tags{ "Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"LightMode" = "ForwardBase"
	}

		ColorMask RGBA
		Cull Off
		ZTest off
		ZWrite off


		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"	

		sampler2D _MainTex;
		sampler2D _Previous;
		sampler2D _DestBuffer;
		float4 _DestBuffer_TexelSize;
		sampler2D _SourceTexture;
		float _Noise;
	//Will also need _DestBuffer
	struct v2f {
		float4 pos : POSITION;
		float2 texcoord : TEXCOORD0;
	};


	v2f vert(appdata_full v) {
		v2f o;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.texcoord = v.texcoord.xy;

		return o;
	}


	inline float random(float2 st) {
		return frac(sin(dot(st.xy + _Time.x, float2(12.9898f, 78.233f)))* 43758.5453123f);
	}

	float4 frag(v2f i) : COLOR{


	//sampler2D _MainTex;
	//sampler2D _Previous;
	//Will also need _DestBuffer

		// EDGES
		float2 duv = i.texcoord.xy;
		float2 courner = duv - 0.5;
		courner *= courner;
		courner = max(0, courner - 0.2);
		float crn = courner.x + courner.y;

		float4 col = tex2Dlod(_SourceTexture, float4(1 - i.texcoord.x, i.texcoord.y, 0, 0));
		col.a = 0;

		float _Noise = 128;

		// Noised Sampling
		duv.x += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.x;
		duv.y += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.y;
		float4 dcoln = tex2Dlod(_DestBuffer, float4(duv, 0, 0));
		float4 dcol0 = tex2Dlod(_DestBuffer, float4(i.texcoord.xy, 0, 0));

		duv = i.texcoord.xy;
		duv.x += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.x;
		duv.y += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.y;

		float4 dcolRight = tex2Dlod(_DestBuffer, float4(duv, 0, 0));

		float nDiff = length(col.rgb - dcoln.rgb)*(1.1 - dcoln.a);

		float curDiff = length(col.rgb - dcol0.rgb)*(1.1 - dcol0.a);

		float rightDiff = length(col.rgb - dcolRight.rgb)*(1.1 - dcolRight.a);

		float useNoised = saturate((curDiff - nDiff) * 1024);

		dcoln = useNoised * dcoln + dcol0 * (1 - useNoised);

		float useRight = saturate((min(curDiff, nDiff) - rightDiff) * 1024);

		dcoln = useRight * dcolRight + dcoln * (1 - useRight);

		float usingCurrent = max(0, 1 - useRight - useNoised);

		float brN = length(dcoln.rgb);

		float3 diff = col.rgb - dcoln.rgb;
		diff *= diff;
		float dist = saturate(diff.x + diff.y + diff.z);
		float difference = (1.1 + (0.2*brN) - dist * 20); //dcoln.a;

		float same = saturate((difference - 1) * 10);// *(1 - usingCurrent) + usingCurrent;

		float alpha = min(dcoln.a, saturate((dcoln.a*same) * 2))*0.99;

		col = dcoln * alpha + col * (1 - alpha);


		duv = i.texcoord.xy;
		duv.x += _DestBuffer_TexelSize.x;
		float4 dcol_0 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));

		duv.x -= _DestBuffer_TexelSize.x * 2;
		float4 dcol_1 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));


		float dp = saturate((dcol_0.a - dcol_1.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_1 * (1 - dp);

		duv.x += _DestBuffer_TexelSize.x;
		duv.y += _DestBuffer_TexelSize.y;
		float4 dcol_2 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));


		dp = saturate((dcol_0.a - dcol_2.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_2 * (1 - dp);


		duv.y -= _DestBuffer_TexelSize.y * 2;
		float4 dcol_3 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));

		dp = saturate((dcol_0.a - dcol_3.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_3 * (1 - dp);


		dp = saturate((dcol_0.a - col.a) * 1024)*max(0,1 - length(col.rgb - dcol_0.rgb) * 8);
		col = dcol_0 * dp + col * (1 - dp);


		col.a = saturate(alpha - crn * 10);

		return  col;
	}
		ENDCG
	}
	}
	}
}