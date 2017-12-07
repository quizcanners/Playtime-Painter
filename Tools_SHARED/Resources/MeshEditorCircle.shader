// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Editor/Grid" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_Size ("Scale", float) = 4
		_dx ("_dx", float) = 0
		_dy ("_dy", float) = 0
	}
	Category {
	Tags {  "Queue"="Overlay+200"
	 		"IgnoreProjector"="True" 
	 		"RenderType"="Transparent" }
	 		
	Blend SrcAlpha One//Blend SrcAlpha OneMinusSrcAlpha//
	ColorMask RGB
	Cull Off
	Lighting Off
	ZWrite Off
	ZTest LEqual//Less | Greater | LEqual | GEqual | Equal | NotEqual | Always
	//ZTest Off
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;
			float _Size;
			float _dx;
			float _dy;

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)  {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord-0.5 ;
				return o;
			}

			fixed4 frag (v2f i) : COLOR {
			_Color.a*=(1-(i.texcoord.x*i.texcoord.x+i.texcoord.y*i.texcoord.y)*4);
		
			i.texcoord+=float2(_dx,_dy);

		//	_Size = i.projPos.z/16;

			float2 perfTex=(floor(i.texcoord*_Size)+0.5)/_Size;
			float2 uv2=abs(i.texcoord-perfTex)-0.48/_Size;
			float smooth = saturate((saturate(uv2.x) + saturate(uv2.y))*_Size*32);
			
				_Color.a=max(0,_Color.a*abs(_Color.a)*smooth);

			return _Color;
			}
			ENDCG 
		}
	}	
}
}
