Shader "Playtime Painter/UI/Rounded/Masked Graphic" {

	Properties{
		[PerRendererData]_MainTex("RGBA", 2D) = "black" {}
		_Mask("Mask", 2D) = "white" {}
		[Toggle(_SUBTRACTIVE)] unlinked("Subtract mask", Float) = 0
		_Edges("Sharpness", Range(0,1)) = 0.5
		[Toggle(_UNLINKED)] unlinked("Linked Corners", Float) = 0
	}
	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Simple"
			"PerEdgeData" = "Linked"
		}

		ColorMask RGBA
		Cull Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile_instancing
				#pragma shader_feature ____  _UNLINKED 
				#pragma shader_feature ___ _SUBTRACTIVE
				#pragma target 3.0

				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 projPos : TEXCOORD1;
					float4 precompute : TEXCOORD2;
					float2 offUV : TEXCOORD3;
					float2 texcoordOverlay : TEXCOORD4;
					float4 color: COLOR;
				};

				float _Edges;
					
				sampler2D _MainTex;
				sampler2D _Mask;
				float4 _Mask_ST;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.texcoordOverlay = TRANSFORM_TEX(v.texcoord.xy, _Mask);
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = 4 - _Edges * 3;
					o.projPos.xy = v.normal.xy;
					o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w = 1 / (1.0001 - o.texcoord.w);
					o.precompute.xy = 1 / (1.0001 - o.projPos.zw);
					o.precompute.z = (1 + _Edges * 32);

					o.offUV.xy = (o.texcoord.xy - 0.5) * 2;

					return o;
				}

				float4 frag(v2f o) : COLOR{

					float dx = abs(ddx(o.texcoord.x));
					float dy = abs(ddy(o.texcoord.y));
					float mip = (dx + dy) * 200;

					_Edges /= 1 + mip * mip; //LOD

					float4 _ProjTexPos = o.projPos;
					float _Courners = o.texcoord.w;
					float deCourners = o.precompute.w;
					float2 uv = abs(o.offUV);

					float4 col = tex2D(_MainTex, o.texcoord.xy) * o.color;

					float2 uv2 = o.texcoordOverlay.xy;
						
					


					float overlay = tex2D(_Mask, uv2).a;
					uv2 = max(0, uv2 - 1) + max(0, -uv2);
					overlay = max(0, overlay - (uv2.x + uv2.y)*256);

					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;

					float2 forFade = uv;

					uv = max(0, uv - _Courners) * deCourners;

					#if _UNLINKED
					forFade *= forFade;
					float clipp = max(0, 1 - max(max(forFade.x, forFade.y), dot(uv, uv)) );
					#else 
					float clipp = max(0, 1 - dot(uv, uv));
					#endif

					clipp = min(1, pow(clipp * o.precompute.z, o.texcoord.z));

					#if _SUBTRACTIVE
						col.a *= clipp * (1 - overlay);
					#else
						col.a *= clipp * overlay;
					#endif
					
					return col;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
