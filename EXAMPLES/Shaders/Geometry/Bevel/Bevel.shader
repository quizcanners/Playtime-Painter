Shader "Playtime Painter/Geometry/Bevel/Pixelated" {
	Properties{
		_MainTex("Base texture", 2D) = "white" {}
	}

	Category{
		Tags{ 
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"DisableBatching" = "True"
			"UVtype" = "Normal"
			"Solution" = "Bevel"
		}

		SubShader{
			Pass {

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma target 3.0

				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;
				//sampler2D _Noise;

				struct v2f {
					float4 pos : SV_POSITION;
					float4 vcol : COLOR0;
					float3 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					float4 edge : TEXCOORD3;
					float3 snormal: TEXCOORD4;
					SHADOW_COORDS(5)
					float3 viewDir: TEXCOORD6;
					float3 edgeNorm0 : TEXCOORD7;
					float3 edgeNorm1 : TEXCOORD8;
					float3 edgeNorm2 : TEXCOORD9;
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
		
					o.texcoord = v.texcoord.xy;
					o.vcol = v.color;
					o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w); //v.texcoord1;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					float3 deEdge = 1 - o.edge.xyz;

					o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
					o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
					o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

					o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

					TRANSFER_SHADOW(o);

					return o;
				}

				float4 frag(v2f i) : SV_Target
				{

					float weight;

					float3 normal = DetectSmoothEdge(i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight); //(i.edge.xyz);

					float mip;
					smoothedPixelsSampling(i.texcoord.xy, _MainTex_TexelSize, mip);

					float4 col = tex2Dlod(_MainTex, float4(i.texcoord.xy,0,mip));

					col = col*(1- weight) + i.vcol*weight;
					float shadow = SHADOW_ATTENUATION(i);

					i.viewDir.xyz = normalize(i.viewDir.xyz);

					Simple_Light(float4 (0, 0, col.a, 1),
						normal, i.viewDir.xyz, col, shadow, 1);
	
					return col;
		
				}
				ENDCG
			}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
		FallBack "Diffuse"
	}
}