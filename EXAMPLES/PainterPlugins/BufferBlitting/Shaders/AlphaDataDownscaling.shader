Shader "PlaytimePainter/BufferBlit/AlphaDataDownscaling" {
	Properties{
		_MainTex("Tex", 2D) = "white" {}
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
#include "UnityCG.cginc"	


		sampler2D _MainTex;
	float4 _MainTex_TexelSize;

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


	float Contrast(float4 col) {
		float avg = (col.r + col.g + col.b) *0.333;

		float len = abs((col.r - avg)*(col.b - avg)*(col.g - avg));

		return len*col.a;
	}

	float4 frag(v2f i) : COLOR{

		//float4 col = tex2Dlod(_MainTex, float4(i.texcoord.xy, 0, 0));

		float2 ts = _MainTex_TexelSize.xy;

		float2 duv = i.texcoord.xy;
		duv.x += ts.x*0.5;
		duv.x += ts.y*0.5;
		float4 dcol_0 = tex2Dlod(_MainTex, float4(duv, 0, 0));

		duv.y -= ts.y;
		float4 dcol_1 = tex2Dlod(_MainTex, float4(duv, 0, 0));

		duv.x += ts.x;
		float4 dcol_2 = tex2Dlod(_MainTex, float4(duv, 0, 0));

		duv.y += ts.y;
		float4 dcol_3 = tex2Dlod(_MainTex, float4(duv, 0, 0));


		float ct0 = Contrast(dcol_0);
		float ct1 = Contrast(dcol_1);
		float ct2 = Contrast(dcol_2);
		float ct3 = Contrast(dcol_3);

		float dp = saturate((ct0 - ct1) * 2048);
		dcol_0 = dcol_0 * dp + dcol_1 * (1 - dp);

		dp = saturate((ct0 - ct2) * 2048);
		dcol_0 = dcol_0 * dp + dcol_2 * (1 - dp);

	
		dp = saturate((ct0 - ct3) * 2048);
		dcol_0 = dcol_0 * dp + dcol_3 * (1 - dp);

		return  dcol_0;
	}
		ENDCG
	}
	}
	}
}