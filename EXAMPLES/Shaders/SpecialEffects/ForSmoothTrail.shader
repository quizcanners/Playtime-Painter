Shader "PlaytimePainter/Basic/ForSmoothTrail" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_Hardness("Hardness", Range(1,16)) = 2
	}

		Category{
		Tags{
		"Queue" = "AlphaTest"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
	}

		Cull Off
		ZWrite Off
		Blend SrcAlpha One

		SubShader{

		Pass{

		CGPROGRAM


#include "UnityCG.cginc"

#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile_fwdbase
#pragma multi_compile_instancing
#pragma target 3.0

		float4 _Color;
	float _Hardness;

	struct v2f {
		float4 pos : SV_POSITION;
		float2 texcoord : TEXCOORD2;

	};


	v2f vert(appdata_full v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.texcoord = v.texcoord.xy;
		return o;
	}



	float4 frag(v2f i) : COLOR{

		i.texcoord.x = pow(1-i.texcoord.x,32);

		float2 off = i.texcoord - 0.5;
		off *= off;

		float alpha = saturate(pow(saturate((1 - (off.x + off.y) * 4)) * _Hardness, _Hardness + 2));

		_Color.a *= alpha;

		return _Color;
	}
		ENDCG

	}
	}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}

}

