Shader "Playtime Painter/ProjectorTest" {

	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		[Toggle(_DEBUG)] debugOn("Debug", Float) = 0
	}

	SubShader{
		Tags { "RenderType" = "Opaque" }
		Pass {
				CGPROGRAM

			#include "PlaytimePainter_cg.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma shader_feature ____ _DEBUG 
			#pragma multi_compile ______ USE_NOISE_TEXTURE

			#pragma target 3.0

			struct v2f {
				float4 pos : 		SV_POSITION;
				float4 worldPos : 	TEXCOORD0;
				float3 normal : 	TEXCOORD1;
				float2 texcoord : 	TEXCOORD2;
				float4 shadowCoords : TEXCOORD6;
				float4 color: 		COLOR;
			};

			uniform float4 _MainTex_ST;
			sampler2D _MainTex;
			sampler2D _Global_Noise_Lookup;

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);

				o.normal.xyz = UnityObjectToWorldNormal(v.normal);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz,1.0f));

				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = v.color;
				o.shadowCoords = mul(pp_ProjectorMatrix, o.worldPos); 
				return o;
			}

			float4 frag(v2f o) : COLOR{

				float camAspectRatio = pp_ProjectorConfiguration.x;
				float camFOVDegrees = pp_ProjectorConfiguration.y;
				//float near = pp_ProjectorConfiguration.z;
				float deFar = pp_ProjectorConfiguration.w;

				o.shadowCoords.xy /= o.shadowCoords.w;

				float alpha = max(0, 1 - dot(o.shadowCoords.xy, o.shadowCoords.xy));

				float viewPos = length(float3(o.shadowCoords.xy * camFOVDegrees,1))*camAspectRatio;

			
				float true01Range = length(o.worldPos - pp_ProjectorPosition.xyz) * deFar;

				//Reverse Distance

				float predictedDepth = 1-(((viewPos / true01Range) - pp_ProjectorClipPrecompute.y) * pp_ProjectorClipPrecompute.z);

				//End Reverse Distance

				// Linear Distance:
				//float range01 = viewPos / (pp_ProjectorClipPrecompute.x * (1 - depth) + pp_ProjectorClipPrecompute.y);

				//return  max(0, alpha - abs(true01Range - range01) * 100);

				float2 uv = (o.shadowCoords.xy + 1) * 0.5;



				#if USE_NOISE_TEXTURE

				float2 off = pp_DepthProjection_TexelSize.xy;

				float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy *12345 + float2(_SinTime.w, _CosTime.w) * 67891, 0, 0));

				float d0 = tex2D(pp_DepthProjection, uv).r - predictedDepth;
				
				uv -= o.shadowCoords.xy*off*(1+noise.xy);

				float d1 = tex2D(pp_DepthProjection, uv - off					* noise.r).r - predictedDepth;
				float d2 = tex2D(pp_DepthProjection, uv + off					* noise.g).r - predictedDepth;
				float d3 = tex2D(pp_DepthProjection, uv + float2(off.x, -off.y)	* noise.b).r - predictedDepth;
				float d4 = tex2D(pp_DepthProjection, uv - float2(off.x, -off.y)	* noise.a).r - predictedDepth;
				
				off *= 1.6;

				float d5 = tex2D(pp_DepthProjection, uv + float2(0,		off.y)	* noise.r).r - predictedDepth;
				float d6 = tex2D(pp_DepthProjection, uv - float2(0,		off.y)	* noise.g).r - predictedDepth;
				float d7 = tex2D(pp_DepthProjection, uv + float2(off.x, 0)		* noise.b).r - predictedDepth;
				float d8 = tex2D(pp_DepthProjection, uv - float2(off.x, 0)		* noise.a).r - predictedDepth;

				float depth = (d0 + d1 + d2 + d3 + d4 + d5 + d6 + d7 + d8)/9;

				float maxD = max(d0, max(max(d1, d2), max(d3, d4))) +noise.x*0.00001;

				float minD = min(d0, min(min(d1, d2), min(d3, d4))) - noise.x*0.00001;

				float ambient = saturate(maxD * 3000);

				ambient = (ambient - abs(max(0, ambient - 0.5)));

				#else
				
				float depth = tex2D(pp_DepthProjection, uv).r - predictedDepth;
				
				#endif

				float shadow = saturate(depth / max(0.005, maxD));

			//	return max(0, minD)*10000;

				//return minD;

				return alpha - max(saturate(depth / max(0.005, maxD)), 
					 max(0,ambient )
						//- max(0, minD)*10000 
						//- shadow
					
					
					) * 0.5;
					//+ 
					//max(0,ambient)
					 //* 1000;

			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}