Shader "Playtime Painter/ReplacementShaderTest" {

	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		[Toggle(_DEBUG)] debugOn("Debug", Float) = 0
	}


		SubShader{
		Tags { "RenderType" = "Opaque" }
		Pass {
				CGPROGRAM

			#include "UnityCG.cginc"
			#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma shader_feature ____ _DEBUG 

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
				float far = pp_ProjectorConfiguration.w;

				o.shadowCoords.xy /= o.shadowCoords.w;

				float alpha = max(0, 1 - dot(o.shadowCoords.xy, o.shadowCoords.xy));

				float3 viewPos = float3(o.shadowCoords.xy * camFOVDegrees,1)*camAspectRatio;

				float depth = tex2D(pp_DepthProjection, (o.shadowCoords.xy + 1) * 0.5);

				float dist = 1.0 / (pp_ProjectorClipPrecompute.x * (1 - depth) + pp_ProjectorClipPrecompute.y);

				dist = length(viewPos * dist);

				float True01Range = length(o.worldPos - pp_ProjectorPosition.xyz) / far;

				return  max(0, alpha - abs(True01Range - dist) * 100);

		
			}
			ENDCG
		}
		}
			FallBack "Diffuse"
}