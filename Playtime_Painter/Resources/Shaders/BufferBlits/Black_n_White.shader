Shader "Editor/BufferBlit/Black_n_White" {
	Properties{
		_MainTex("Tex", 2D) = "white" {}
		_Power("Black And White Detection", float) = 1
		_Brighten("Brighten Amount", float) = 1
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
		float _Power;
		float _Brighten;

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

		float grey = (col.r + col.g + col.b) / 3;

		grey = pow(grey, _Power)*_Brighten;

		return grey;
	}
		ENDCG
	}
	}
		}
}