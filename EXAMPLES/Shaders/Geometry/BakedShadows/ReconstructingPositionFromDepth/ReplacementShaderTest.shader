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
			sampler2D c_ShadowTex;
			float4x4 c_ShadowMatrix;
			float4 c_ShadowCamPos;
			float4 c_ZBufferParameters;
			float4 c_CamParams;

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);

				o.normal.xyz = UnityObjectToWorldNormal(v.normal);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz,1.0f));
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = v.color;
				o.shadowCoords = mul(c_ShadowMatrix, o.worldPos); //mul(c_ShadowMatrix, float4(o.worldPos, 1.0));


				// All good above this line

				float4 mvp = mul(c_ShadowMatrix, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)));






				return o;
			}

			float4 frag(v2f o) : COLOR{

				if (o.shadowCoords.w < 0)
				return 0;

				o.shadowCoords.xy /= o.shadowCoords.w;

				float2 uv = o.shadowCoords.xy;

				o.shadowCoords.xy = (o.shadowCoords.xy+1) * 0.5;

				if (o.shadowCoords.x < 0 || o.shadowCoords.x>1 || o.shadowCoords.y < 0 || o.shadowCoords.y>1)
					return 0;

				float tex = tex2D(c_ShadowTex, o.shadowCoords);



				float camAspectRatio = c_CamParams.x;
				float camFOVDegrees = c_CamParams.y;
				float near = c_CamParams.z; 
				float far = c_CamParams.w; 

				float3 vec = o.worldPos - c_ShadowCamPos.xyz;

				float trueDist =  length(vec);
				
				float True01Range = trueDist / far;

				if (True01Range > 1 || True01Range < 0)
					True01Range = 0;

				if (tex > 1 || tex < 0)
					tex = 0;

				float dist = 1.0 / (c_ZBufferParameters.x * (1 - tex) + c_ZBufferParameters.y); // Is a 01 depth

				float2 viewPosXY = uv.xy;
				


				const float deg2rad = 0.0174533;

				float viewHeight = dist * tan(camFOVDegrees * 0.5 * deg2rad);

				viewPosXY.y *=  viewHeight;

				viewPosXY.x *=  viewHeight * camAspectRatio;

				dist = length(float3(viewPosXY.xy, dist)); 

				tex = 1 - abs(True01Range-dist)*100;

				return tex;

		
			}
			ENDCG
		}
		}
			FallBack "Diffuse"
}