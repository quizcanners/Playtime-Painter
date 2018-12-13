Shader "Playtime Painter/Buffer Blit/Alpha Data Smoothing" {
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

	float4 frag(v2f i) : COLOR{

		float4 col = tex2Dlod(_MainTex, float4( i.texcoord.xy, 0, 0));

		float2 ts = _MainTex_TexelSize.xy;

		float2 duv = i.texcoord.xy;
		duv.x += ts.x;
		float4 dcol_0 = tex2Dlod(_MainTex, float4(duv, 0, 0));

		duv.x -= ts.x * 2;
		float4 dcol_1 = tex2Dlod(_MainTex, float4(duv, 0, 0));

		float dp = saturate((dcol_0.a - dcol_1.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_1 * (1 - dp);

		duv.x += ts.x;
		duv.y += ts.y;
		float4 dcol_2 = tex2Dlod(_MainTex, float4(duv, 0, 0));

		dp = saturate((dcol_0.a - dcol_2.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_2 * (1 - dp);

		duv.y -= ts.y * 2;
		float4 dcol_3 = tex2Dlod(_MainTex, float4(duv, 0, 0));

		dp = saturate((dcol_0.a - dcol_3.a) * 1024);
		dcol_0 = dcol_0 * dp + dcol_3 * (1 - dp);

		dp = saturate((dcol_0.a - col.a) * 1024)*max(0,1 - length(col.rgb - dcol_0.rgb) * 8);
		col = dcol_0 * dp + col * (1 - dp);

		return  float4(1, 0, 0, 1);// col;
	}
		ENDCG
	}
	}
	}
}