Shader "Playtime Painter/UI/Rounded/BumpedButton" {
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "gray" {}
		[NoScaleOffset]_NoiseTex("Albedo (RGB)", 2D) = "gray" {}
		_Edges("Edge Sharpness", Range(0.1,1)) = 0.5
		_LightDirection("Light Direction Vector", Vector) = (0,0,0,0)
		[Toggle(_UNLINKED)] unlinked("Linked Corners", Float) = 0
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Hide"
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
				#pragma multi_compile ____  _UNLINKED 		

				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float3 lightProjection : TEXCOORD1;
					float4 projPos : TEXCOORD2;
					float4 precompute : TEXCOORD3;
					float4 offUV : TEXCOORD4;
					float4 color: COLOR;
				};

				sampler2D _NoiseTex;
				float _Edges;
				float4 _LightDirection;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;
					o.lightProjection = normalize(_LightDirection);

		
					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = 3 - _Edges * 2;
					o.projPos.xy = v.normal.xy;
					o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					o.precompute.w = 1 / (1.0001 - o.texcoord.w);
					o.precompute.xy = 1 / (1.0001 - o.projPos.zw);
					o.precompute.z = (1 + o.texcoord.z *  (16 - _Edges * 15));


					o.offUV.xy = (o.texcoord.xy - 0.5) * 2;
					o.offUV.zw = float2((o.offUV.x + _SinTime.x + o.offUV.y*0.7) * 987.432, (o.offUV.y + _CosTime.x + o.offUV.x*0.23) * 123.456);

					return o;
				}

				float4 frag(v2f o) : COLOR{

					float4 _ProjTexPos =	o.projPos;
					float _Courners =		o.texcoord.w;
					float deCourners =		o.precompute.w;
					float2 uv =				abs(o.offUV);
					
					uv = max(0, uv - _ProjTexPos.zw) * o.precompute.xy;

					float2 forFade = uv;

					uv = max(0, uv - _Courners) * deCourners;

					#if _UNLINKED
					forFade *= forFade;
					float clipp = max(0, 1 - max(max(forFade.x, forFade.y), dot(uv, uv)));
					#else 
					float clipp = max(0, 1 - dot(uv, uv));
					#endif

					clipp = min(1, pow(clipp * o.precompute.z, o.texcoord.z));
					o.color.a *= clipp;

					float4 noise = tex2Dlod(_NoiseTex, float4(o.offUV.zw, 0, 0));
					
					float2 dir = o.texcoord.xy - 0.5;
					dir = uv.xy * sign(dir) + (noise.xy - 0.5)*0.1;

					float3 norm = normalize(float3(dir.x, dir.y, 0.5));

					float angle = max(0,dot(norm, o.lightProjection));

					o.color.rgb *= 0.8+ min(1, angle)*0.3;

					o.color.rgb += angle * angle*(0.2+noise.y*0.01);

					return o.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
