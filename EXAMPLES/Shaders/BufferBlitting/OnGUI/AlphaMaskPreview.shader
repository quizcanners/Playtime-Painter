Shader "Playtime Painter/Buffer Blit/GUI/Alpha Mask Preview" {
	Properties{
		_MainTex("Tex", 2D) = "white" {}
	_Power("Power", Range (1,8)) = 1
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

		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"	

		sampler2D _MainTex;
	float _Power;

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

		float4 col = tex2Dlod(_MainTex, float4(i.texcoord.xy, 0, 0));
	
		float alpha = pow(col.a, _Power)*_Power;
		col.rgb = 1;
		col.a = alpha;

		return  col;
	}
		ENDCG
	}
	}
	}
}