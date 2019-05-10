Shader "Playtime Painter/Pixel Art/Parallax" {
	Properties {
			[NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
			//_Size ("Scale", float) = 4
			_parallax ("Parallax", Range (0,5))=0
	}

	SubShader {
		Tags { 
	 		"RenderType"="Transparent"
			"Queue"="Transparent+30"
		}

		LOD 200
		Cull Off
		
		CGPROGRAM
		#pragma surface surf Lambert alpha
		#pragma target 3.0

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		float _parallax;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			float2 v=IN.viewDir.xy/IN.viewDir.z;
			float2 uv=IN.uv_MainTex;
			float2 perfTex=(floor(uv*_MainTex_TexelSize.z)+0.5)*_MainTex_TexelSize.x;

			float4 c = tex2Dlod (_MainTex, float4(perfTex,0,0));  // Upper Color
		
			uv-=v*(1-c.a)*_parallax * 0.01;
			float2 perfTex2=(floor((uv)*_MainTex_TexelSize.z)+0.5)*_MainTex_TexelSize.x;

			float2 uv2=abs(uv-perfTex2)-0.45*_MainTex_TexelSize.x;

			float smooth2 = saturate((saturate(uv2.x) + saturate(uv2.y))*_MainTex_TexelSize.z *32);

			float4 c2 = tex2Dlod (_MainTex, float4(perfTex2,0,0)); // lover color
					
			// Add Upper is diagonal 

			float2 corner=((perfTex+perfTex2)/2-IN.uv_MainTex)*_MainTex_TexelSize.z;

			float Xtest = (corner.x/v.x - corner.y/v.y);

			perfTex2-=perfTex;
			perfTex2=abs(perfTex2);
			float isDiagonal = perfTex2.x*perfTex2.y; // GOOOD, don't toch it!!!
			v/=abs(v)*_MainTex_TexelSize.z;
			isDiagonal*= saturate(tex2Dlod (_MainTex,
				float4(perfTex.x-saturate(Xtest)*v.x, perfTex.y-saturate(-Xtest)*v.y,0,0)).a - c.a);

			// Use other color if different pixel
			float pixDist=saturate((perfTex2.x+perfTex2.y)*128);
			c.rgb=c.rgb*(1-pixDist)+c2.rgb*(pixDist);

			float4 trans=tex2Dlod (_MainTex, float4(IN.uv_MainTex,0,0));
			float upper=saturate((c.a-c2.a+0.01)*128-isDiagonal*2048* _MainTex_TexelSize.z)*(1-smooth2);
		
			o.Albedo = (c.rgb*upper+trans.rgb*(1-upper)/2);  
			
			//*************************************
			o.Normal = UnpackNormal(0.5);
			o.Alpha =saturate((1-c.a*c.a*(0.5+trans.a/2))*2); 
			//!!!!!!!!!!!!!! Replace trans.a to +((1-trans.a)*smooth for clarer effect
		}
		ENDCG
	}
	FallBack "Diffuse"
}
