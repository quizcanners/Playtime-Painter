Shader "Painter_Experimental/BufferBlit/Black_n_White" {
	Properties{
		_MainTex("Tex", 2D) = "white" {}
		//_Power("Black And White Detection", float) = 1
		_Noise("Noise Amount", float) = 1
	}
		Category{
		Tags{ "Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"LightMode" = "ForwardBase"
	}

		ColorMask RGBA
		Cull Back
		ZTest off
		ZWrite off


		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Assets/Tools/Playtime_Painter/Resources/Shaders/qc_Includes.cginc"

		sampler2D _MainTex;
		float _Noise;

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

	float4 frag(v2f i) : COLOR{


		// EDGES
		float2 duv = i.texcoord.xy;
		float2 courner = duv - 0.5;
		courner *= courner;
		courner = max(0, courner - 0.2);
		float crn = courner.x + courner.y;

		float4 col = tex2Dlod(_SourceTexture, float4(1 - i.texcoord.x, i.texcoord.y, 0, 0));
		col.a = 0;

		_Noise = 64;

		// Noised Sampling
		duv.x += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.x;
		duv.y += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.y;
		float4 dcoln = tex2Dlod(_DestBuffer, float4(duv, 0, 0));
		float4 dcol0 = tex2Dlod(_DestBuffer, float4(i.texcoord.xy, 0, 0));

		duv = i.texcoord.xy;
		duv.x += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.x;
		duv.y += (random(duv) * 2 - 1)*_Noise*_DestBuffer_TexelSize.y;

		float4 dcolRight = tex2Dlod(_DestBuffer, float4(duv, 0, 0));


		float3 colnorm = normalize(col.rgb);
		float3 nnorm = normalize(dcoln.rgb);
		float3 zeronorm = normalize(dcol0.rgb);
		float3 rnorm = normalize(dcolRight.rgb);

		float nDiff = length(colnorm.rgb - nnorm.rgb)*(1.1- dcoln.a);

		float curDiff = length(colnorm.rgb - zeronorm.rgb)*(1.1 - dcol0.a);

		float rightDiff = length(colnorm.rgb - rnorm.rgb)*(1.1 - dcolRight.a);



		float useNoised = saturate((curDiff - nDiff) * 1024);

		dcoln = useNoised * dcoln + dcol0 * (1 - useNoised);

		float useRight = saturate((min(curDiff, nDiff) - rightDiff) * 1024);

		dcoln = useRight * dcolRight + dcoln * (1 - useRight);


		bool usingOther = saturate(useNoised + useRight);


		float brN = length(dcoln.rgb);

		float3 diff = col.rgb - dcoln.rgb;
		//diff = pow(diff,2);
		float dist = saturate(diff.x + diff.y + diff.z);
	//	float difference = (1.1+(0.1*brN) - dist*128); 
		
	//	float same = saturate((difference - 1) * 10);

		float alpha = saturate(dcoln.a - dist)*(0.992+(saturate(brN) + usingOther)*0.004);

		col =  dcoln * alpha + col * (1 - alpha);
		

		duv = i.texcoord.xy;
		duv.x += _DestBuffer_TexelSize.x;
		float4 dcol_0 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));

		duv.x -= _DestBuffer_TexelSize.x*2;
		float4 dcol_1 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));


		float dp = saturate((dcol_0.a - dcol_1.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_1 * (1 - dp);

		duv.x += _DestBuffer_TexelSize.x;
		duv.y += _DestBuffer_TexelSize.y;
		float4 dcol_2 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));


		dp = saturate((dcol_0.a - dcol_2.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_2 * (1 - dp);


		duv.y -= _DestBuffer_TexelSize.y*2;
		float4 dcol_3 = tex2Dlod(_DestBuffer, float4(duv, 0, 0));

		dp = saturate((dcol_0.a - dcol_3.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_3 * (1 - dp);


		dp = saturate((dcol_0.a - col.a) * 1024)*saturate(1 - length(col.rgb - dcol_0.rgb)*8);
		col = dcol_0 * dp + col * (1 - dp);

		/*alpha = max(alpha,
			min(min(dcol_3.a,dcol_1.a),
				min(dcol_2.a,dcol_0.a)));*/

		col.a = saturate(alpha);// -crn * 10);

		return  col;
	}
		ENDCG
	}
	}
		}
}