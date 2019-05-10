Shader "Playtime Painter/Effects/RoundLightRaysAnimated" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	}

		Category{

			ColorMask RGB
			Cull Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			SubShader{

				Tags{
					"Queue" = "AlphaTest"
					"IgnoreProjector" = "True"
					"RenderType" = "Transparent"
				}

				Pass{

					CGPROGRAM

					#include "UnityCG.cginc"

					#pragma vertex vert
					#pragma fragment frag
					#pragma multi_compile_fwdbase
					#pragma multi_compile_instancing
					#pragma target 3.0

					struct v2f {
						float4 pos : SV_POSITION;
						float2 texcoord : TEXCOORD2;
						float4 screenPos : TEXCOORD5;
						float4 color: COLOR;
					};

					v2f vert(appdata_full v) {
						v2f o;
						UNITY_SETUP_INSTANCE_ID(v);
						o.pos = UnityObjectToClipPos(v.vertex);
						o.texcoord = v.texcoord.xy;
						o.screenPos = ComputeScreenPos(o.pos);
						o.color = v.color;
						return o;
					}

					sampler2D _MainTex;

					float4 frag(v2f i) : COLOR{

						float2 off = i.texcoord - 0.5;
					
						float2 rotUV = off;
						float si = _SinTime.w;
						float co = _CosTime.w;

						float tx = rotUV.x;
						float ty = rotUV.y;
						rotUV.x = (co * tx) - (si * ty);
						rotUV.y = (si * tx) + (co * ty);
						rotUV += 0.5;

						float4 col = i.color * tex2D(_MainTex, rotUV);

						rotUV = off;
						si = _SinTime.x;
						co = _CosTime.x;

						tx = rotUV.x;
						ty = rotUV.y;
						rotUV.x = (co * tx) - (si * ty);
						rotUV.y = (si * tx) + (co * ty);
						rotUV += 0.5;

						rotUV.y = 1 - rotUV.y;

						col.a *= tex2D(_MainTex, rotUV).a;
					
						off *= off;
						col.a*=saturate(1 - (off.x + off.y) * 4);

						return col;
					}
					ENDCG
				}
			}
			Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}