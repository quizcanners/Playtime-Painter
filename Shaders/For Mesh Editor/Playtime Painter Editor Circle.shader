Shader "Playtime Painter/Editor/Markers/Circle" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
	}

	Category {
		Tags { 
			"Queue"="Overlay+1"
	 		"RenderType"="Transparent" 
		}
	 		
		Blend SrcAlpha One
		ColorMask RGB
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Off
	
		SubShader {
			Pass {
		
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				float4 _Color;
			  
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
					o.texcoord = v.texcoord-0.5;
					return o;
				}

				float4 frag (v2f i) : COLOR {
					_Color.a*=(1-(i.texcoord.x*i.texcoord.x+i.texcoord.y*i.texcoord.y)*4);
					_Color.a=max(0,_Color.a*abs(_Color.a*_Color.a));
					return _Color;
				}
				ENDCG 
			}
		}	
	}
}







	