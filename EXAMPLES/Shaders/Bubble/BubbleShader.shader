Shader "Painter_Experimental/BubbleShader" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}

	}
		SubShader
		{
			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 100

			ZWrite Off
		Cull Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc" 
		

		struct v2f
		{
		float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			float3 viewDir: TEXCOORD1;
			float3 normal : TEXCOORD2;
			float3 worldPos : TEXCOORD3;
			float4 wTangent : TEXCOORD10;
		};

		sampler2D _MainTex;


		v2f vert(appdata_full v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			o.normal.xyz = UnityObjectToWorldNormal(v.normal);
			o.viewDir.xyz = WorldSpaceViewDir(v.vertex);
			o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
			o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
			return o;
		}

		float4 frag(v2f i) : SV_Target
		{

			float2 uv = i.uv - 0.5;
			float2 uvs = uv * uv;

			//float3 normal = normalize();

			float3 tnormal = normalize(float3(uv.x, uv.y, 0.5-sqrt(uvs.x + uvs.y)));

			float3 wBitangent = cross(i.normal, i.wTangent.xyz) * i.wTangent.w;

			float3 tspace0 = float3(i.wTangent.x, wBitangent.x, i.normal.x);
			float3 tspace1 = float3(i.wTangent.y, wBitangent.y, i.normal.y);
			float3 tspace2 = float3(i.wTangent.z, wBitangent.z, i.normal.z);

			i.normal.x = dot(tspace0, tnormal);
			i.normal.y = dot(tspace1, tnormal);
			i.normal.z = dot(tspace2, tnormal);


			//float3 halfDirection = normalize(i.viewDir.xyz + _WorldSpaceLightPos0.xyz);

			//float NdotH = saturate((dot(i.normal, halfDirection) - 1  + 0.01) * 100);


			float3 preDot = i.normal * i.viewDir.xyz;
			float dotprod = (preDot.x + preDot.y + preDot.z);
			float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal);

			float dott = max(0.1, dot(_WorldSpaceLightPos0, -reflected));

			float4 col = tex2D(_MainTex, i.uv);
		
		// += (1 - col.a) + NdotH * _LightColor0.rgb;


			uv *= uv;
			float rad = (uv.x + uv.y) * 4;
			
			float trim = saturate((1 - rad) * 10);

			col.a *= trim;
			col.rgb *= trim;
			
			col.rgb += pow(dott, 8) * 10 * _LightColor0.rgb*col.a;

		return col;
		}
			ENDCG
		}
		}
}