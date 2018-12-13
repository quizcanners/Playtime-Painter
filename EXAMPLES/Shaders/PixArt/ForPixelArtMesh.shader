Shader "Playtime Painter/Pixel Art/ForPixelArtMesh" {
	Properties {
		_MainTex("_MainTex", 2D) = "white" {}
	//_ExplodedTex ("_ExplodedTex", 2D) = "white" {}
	[NoScaleOffset]_Bump("_bump", 2D) = "bump" {}
	_Smudge("_smudge", 2D) = "gray" {}
	[NoScaleOffset]_BumpEx("_bumpEx", 2D) = "bump" {}
	_BumpDetail("_bumpDetail", 2D) = "bump" {}

		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma vertex vert
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Bump;
		sampler2D _BumpDetail;
		sampler2D _BumpEx;
		sampler2D _Smudge;
		float4 _MainTex_TexelSize;
		uniform float4 _MainTex_ST;
		float _Glossiness;
		float _Metallic;
		//float4 _touchPoint;
		//float4 _OutlineColor;

		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpDetail;
			float2 uv_Smudge;
			float2 texcoord : TEXCOORD1;
			float4 hold : TEXCOORD2;
			float2 perfuv : TEXCOORD3;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);

			o.perfuv.xy = (floor(v.texcoord.zw*_MainTex_TexelSize.z) + 0.5)* _MainTex_TexelSize.x;

			o.texcoord.xy = v.texcoord.xy*_MainTex_ST.xy + _MainTex_ST.zw;

			float2 up = (v.texcoord.zw)*_MainTex_TexelSize.z;
			float2 bord = up;
			up = floor(up);
			bord = bord - up - 0.5;
			float2 hold = bord * 2;
			hold *= _MainTex_TexelSize.x;
			up = (up + 0.5)* _MainTex_TexelSize.x;

			float4 c = tex2Dlod(_MainTex, float4(up, 0, 0));
			float4 contact = tex2Dlod(_MainTex, float4(up + float2(hold.x, 0), 0, 0));
			float4 contact2 = tex2Dlod(_MainTex, float4(up + float2(0, hold.y), 0, 0));
			float4 contact3 = tex2Dlod(_MainTex, float4(up + float2(hold.x, hold.y), 0, 0));

			hold *= _MainTex_TexelSize.z / 5.5;

			bord = abs(bord);

			float4 difff = abs(contact - c);
			float xsame = saturate((0.2 - (difff.r + difff.g + difff.b + difff.a)) * 165800);
			difff = abs(contact2 - c);
			float ysame = saturate((0.2 - (difff.r + difff.g + difff.b + difff.a)) * 165800);
			difff = abs(contact3 - c);
			float ddiff = saturate((0.1 - (difff.r + difff.g + difff.b + difff.a)) * 165800);

			float diag = saturate((1 - ddiff)*xsame*ysame * 165800);

			o.hold.z = diag;

			o.hold.w = 1 - diag;

			o.hold.x = 1 - saturate(xsame);
			o.hold.y = 1 - saturate(ysame);

		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		//UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		//UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input i, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			float2 off = (i.texcoord.xy - i.perfuv.xy);

		

			/*float2 perfTex = (floor(i.texcoord.xy*_MainTex_TexelSize.z) + 0.5) * _MainTex_TexelSize.x;
			float2 off = (i.texcoord.xy - i.perfuv);
			off = off * saturate((abs(off) * _MainTex_TexelSize.z) * 20 - 19);
			perfTex += off;*/

			//off = (i.texcoord.xy - i.perfuv.xy);

			float2 bumpUV = off * _MainTex_TexelSize.zw;

			float4 c = tex2Dlod(_MainTex, float4(i.perfuv.xy, 0, 0));

			float2 border = (abs(float2(bumpUV.x, bumpUV.y)) - 0.4) * 10;
			float bord = max(0, max(border.x*i.hold.x, border.y*i.hold.y)*i.hold.w + i.hold.z*min(border.x, border.y));

			float deBord = 1 - bord;

			bumpUV.x = bumpUV.x*max(i.hold.x, i.hold.z);
			bumpUV.y = bumpUV.y*max(i.hold.y, i.hold.z);

			bumpUV *= 0.98;
			bumpUV += 0.5;
			

			float3 nn = UnpackNormal(
				tex2Dlod(_Bump, float4(bumpUV
					, 0, 0)) *(1 - i.hold.z)
				+ tex2Dlod(_BumpEx, float4(bumpUV, 0, 0)) *(i.hold.z)
			);

			float3 nn2 = UnpackNormal(tex2Dlod(_BumpDetail, float4(i.uv_BumpDetail*(4 + nn.xy), 0, 0)));
			nn += nn2 * ( 2 - c.a)*0.05;

			nn = normalize(nn*(1 - bord) + float3(0, 0, bord));

			o.Normal = nn;

			float smudge = tex2D(_Smudge, i.uv_Smudge).a;
			float deSmudge = 1 - smudge;


			//float dist = length(i.texcoord.xy - i.texcoord.zw + nn.xy * 0.1);

			float2 sat = abs(off) * 128 * _MainTex_TexelSize.zw;
			float2 pixuv = i.perfuv.xy + off * min(1, sat*0.03);

			float4 light = (tex2Dlod(_MainTex, float4(pixuv + (nn.xy)*(_MainTex_TexelSize.xy * (1+0.3* smudge)), 0, 0)));

			float gloss = _Glossiness * smudge*deBord + bord;

			float4 col = ((c + c * light *(1 - gloss)+ light * gloss*0.5))*(1 - bord);

			float3 bgr = col.gbr + col.brg;
			bgr *= bgr;

			col.rgb += bgr * 0.1;

				
			o.Albedo =
				//bord
				// nn
				col.rgb
				//i.hold.rgb;
			//light.rgb
			;

			//o.Emission = o.Albedo;

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = gloss;
			o.Occlusion = 1-bord;
			o.Alpha = col.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
