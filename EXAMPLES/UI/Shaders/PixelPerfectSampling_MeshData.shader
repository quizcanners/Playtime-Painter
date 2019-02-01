Shader "Playtime Painter/UI/PixelPerfectSampling_MeshData" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_OutlineGradient("Outline Gradient", 2D) = "black" {}
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Position"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
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
					float4 texcoord : TEXCOORD2;
					float4 screenPos : TEXCOORD5;
					float4 projPos : TEXCOORD6;
					float4 color: COLOR;
				};

				sampler2D _MainTex;
				sampler2D _OutlineGradient;
				float4 _MainTex_TexelSize;
				

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.screenPos = ComputeScreenPos(o.pos);
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy; 
					o.texcoord.z = abs(o.texcoord.z);
					o.projPos.xy = v.normal.xy; 
					o.projPos.zw = max(0, float2(v.normal.z, -v.normal.z));
			
				//	o.color = v.texcoord2.x;

					return o;
				}


				float4 frag(v2f i) : COLOR{

					float4 _ProjTexPos = i.projPos;
					float _Edge = i.texcoord.z;
					float _Courners = i.texcoord.w;
				
					float2 screenUV = i.screenPos.xy / i.screenPos.w;

					float2 inPix = (screenUV - _ProjTexPos.xy)*_ScreenParams.xy;
					float2 texUV = inPix * _MainTex_TexelSize.xy
						// Just in case a texture is not divisible by 2
						+ _MainTex_TexelSize.xy*0.5*(_MainTex_TexelSize.zw % 2);

					float4 col = tex2D(_MainTex, texUV + 0.5);  // Offset by 0.5 here if is not centered properly

					// Rounded Courners
					float _Blur = (1 - i.color.a);
					float2 uv = abs(i.texcoord.xy - 0.5) * 2;
					uv = max(0, uv - _ProjTexPos.zw) / (1.0001 - _ProjTexPos.zw) - _Courners;
					float deCourners = 1.0001 - _Courners;
					uv = max(0, uv) / deCourners;
					uv *= uv;
					float clipp = max(0, (1 - uv.x - uv.y));

					float upEdge =  saturate(1 + _Edge);

					float uvy = saturate(clipp * (10 - _Courners * 9)* upEdge);

					float4 outline = tex2D(_OutlineGradient, float2(0, uvy));

					outline.a = saturate(outline.a * (1 - uvy) * 8 * upEdge);

					col.rgb *= i.color.rgb;

					col.rgb = col.rgb*(1 - outline.a) + outline.rgb* (outline.a);

					col.a = max(col.a, outline.a);

					col.a *= min(clipp * upEdge *(1 - _Blur) * deCourners * 128, 1)*i.color.a; 

					return col;
				}
			ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
