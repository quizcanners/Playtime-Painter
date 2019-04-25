Shader "Playtime Painter/Geometry/Baked Shadows/Volume Smoke Effect" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Thickness("Thickness", Range(0,20)) = 0.0
	}
	Category {

		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		//Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader {
			Pass {

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile ___ USE_NOISE_TEXTURE
				#pragma target 3.0
				#include "Assets/Tools/quizcanners/quizcanners_cg.cginc"

				uniform sampler2D _MainTex;
				uniform sampler2D g_BakedShadow_VOL;

				float _Thickness;
				float4  _MainTex_ST;
				float4 g_BakedShadows_VOL_TexelSize;


				struct v2f {
					float4 pos : SV_POSITION;
					float3 worldPos : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float2 texcoord : TEXCOORD2;
					SHADOW_COORDS(3)
					float3 viewDir: TEXCOORD4;
					UNITY_FOG_COORDS(5)
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

					o.texcoord = v.texcoord.xy;

					UNITY_TRANSFER_FOG(o, o.pos);
					TRANSFER_SHADOW(o);

					return o;
				}

				float4 frag(v2f o) : COLOR{

					float2 off = o.texcoord - 0.5;
					off *= off;

					o.viewDir.xyz = normalize(o.viewDir.xyz);

					float distance = (10 - max(0,10 - length(_WorldSpaceCameraPos - o.worldPos)))*0.1;

					float alpha = max(0, (1 - (off.x + off.y) * 4)*abs(dot(o.viewDir.xyz, o.normal.xyz)))*distance;

					float2 tc = TRANSFORM_TEX(o.texcoord, _MainTex);
					float4 col = tex2D(_MainTex, tc + _Time.x*0.035) * tex2D(_MainTex, tc*2.5 - _Time.x*0.02f);

					#if USE_NOISE_TEXTURE

					float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.texcoord.xy * 13.5
						+ float2(_SinTime.w, _CosTime.w) * 32, 0, 0));

					col *= (0.8 + noise * 0.2);

					#endif

					col.a *= alpha;

					col.a *= col.a;

					float ambientBlock = col.a;

					float3 normal = -o.viewDir.xyz;

					float3 thickness = normal * _Thickness * ambientBlock;

					float4 bake = 1 - SampleVolume(g_BakedShadow_VOL, o.worldPos, g_VOLUME_POSITION_N_SIZE,  g_VOLUME_H_SLICES, thickness);

					float4 bake2 = 1 - SampleVolume(g_BakedShadow_VOL, o.worldPos, g_VOLUME_POSITION_N_SIZE, g_VOLUME_H_SLICES, -thickness);

					float4 directBake = (saturate((bake - 0.5) * 2) + saturate((bake2 - 0.5) * 2))*(ambientBlock);

					bake = (bake + bake2) * 0.5;

					float3 scatter = 0;
					float3 directLight = 0;

					// Point Lights

					PointLightTransparent(scatter, directLight, o.worldPos.xyz - g_l0pos.xyz,
						 o.viewDir.xyz, ambientBlock, bake.r, directBake.r, g_l0col);

					PointLightTransparent(scatter, directLight, o.worldPos.xyz - g_l1pos.xyz,
						 o.viewDir.xyz, ambientBlock, bake.g, directBake.g, g_l1col);

					PointLightTransparent(scatter, directLight, o.worldPos.xyz - g_l2pos.xyz,
						 o.viewDir.xyz, ambientBlock, bake.b, directBake.b, g_l2col);

					scatter *= (1 - bake.a);

					DirectionalLightTransparent(scatter, directLight, directBake.a,	normal, o.viewDir, ambientBlock, bake.a);

					col.rgb *= (directLight + scatter);

					BleedAndBrightness(col, 1);

					UNITY_APPLY_FOG(o.fogCoord, col);

					return col;

				}
				ENDCG

			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

