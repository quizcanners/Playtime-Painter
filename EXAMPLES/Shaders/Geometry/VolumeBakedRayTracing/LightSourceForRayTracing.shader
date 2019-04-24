Shader "Playtime Painter/Effects/Ray Trace Emission Vertex Color Mask" {

	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	}

	SubShader {

		Tags{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Emmissive"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha One

		Pass{

			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma target 3.0

			struct v2f {
				float4 pos : 		SV_POSITION;
				float3 worldPos : 	TEXCOORD0;
				float3 normal : 	TEXCOORD1;
				float2 texcoord : 	TEXCOORD2;
				float3 viewDir: 	TEXCOORD3;
				float4 screenPos : 	TEXCOORD4;
				float4 fromCenter : TEXCOORD5;
				float4 color: 		COLOR;
			};

			uniform float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);

				o.normal.xyz = UnityObjectToWorldNormal(v.normal);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.viewDir.xyz = WorldSpaceViewDir(v.vertex);
				o.fromCenter = v.texcoord - 0.5;
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.screenPos = ComputeScreenPos(o.pos);
				o.color = v.color;
				return o;
			}

			float4 frag(v2f o) : COLOR{

				float2 off = o.fromCenter;
				off *= off;

				o.viewDir.xyz = normalize(o.viewDir.xyz);
				float2 duv = o.screenPos.xy / o.screenPos.w;

				float4 col = o.color * tex2D(_MainTex, o.texcoord.xy).r;

				col.a *= saturate(1 - (off.x + off.y) * 4);

				return col;
			}
			ENDCG
		}
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"

}