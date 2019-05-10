// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Playtime Painter/Pixel Art/BumpedOutline" {
	Properties {
		[NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset]_Bump ("Bump (RGB)", 2D) = "white" {}
		_BumpDetail("_bumpDetail", 2D) = "bump" {}
		_Smudge("_smudge", 2D) = "gray" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
			_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader {
		Tags {"RenderType" = "Opaque"}

		LOD 200
		
		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows
		//#pragma surface surf Standard fullforwardshadows alpha
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Bump;
		sampler2D _BumpDetail;
		sampler2D _Smudge;
		float _Glossiness;
		float _Metallic;

		float4 _MainTex_TexelSize;

		struct Input {
			float2 uv_MainTex;
			float2 uv_Smudge;
			float2 uv_BumpDetail;
		};

		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {

			float2 up=IN.uv_MainTex*_MainTex_TexelSize.z;
			float2 border=up;
			up=floor(up);
			border=border-up-0.5;
			float2 hold=border*2;
			hold*= _MainTex_TexelSize.x;
			up=(up+0.5)* _MainTex_TexelSize.x;
		
			float2 off = (IN.uv_MainTex - up);

			float4 c = tex2Dlod (_MainTex, float4(up,0,0));

			float4 contact  = tex2Dlod (_MainTex, float4(up+float2(hold.x,0),0,0));
			float4 contact2 = tex2Dlod (_MainTex, float4(up+float2(0,hold.y),0,0));
			float4 contact3 = tex2Dlod (_MainTex, float4(up+float2(hold.x,hold.y),0,0));
	
			hold*= _MainTex_TexelSize.z /5.5;
		
			border=abs(border);

			float4 diff=abs(contact-c);
			float xsame=saturate((0.3-(diff.r+diff.g+diff.b+diff.a))*165800);
			 diff=abs(contact2-c);
			float ysame=saturate((0.3-(diff.r+diff.g+diff.b+diff.a))*165800);
			 diff=abs(contact3-c);
			float ddiff=saturate(((diff.r+diff.g+diff.b+diff.a)-0.3)*165800);

	
			ddiff=saturate(
			ddiff
			*xsame
			*ysame

			 *165800);
			float DeDiff=(1-ddiff);

			border.x*=((1-xsame)*DeDiff+ddiff); 
			border.y*=((1-ysame)*DeDiff+ddiff); 

			float XaboveY=saturate((border.x-border.y)*165800);
			float YaboveX=1-XaboveY;

			contact=(contact2*YaboveX+contact*XaboveY)*DeDiff+contact3*ddiff;

			hold.x*=(XaboveY*ddiff+xsame*DeDiff); 
			hold.y*=(YaboveX*ddiff+ysame*DeDiff); 

			border.x*=(YaboveX*ddiff+XaboveY*DeDiff); 
			border.y*=(XaboveY*ddiff+YaboveX*DeDiff); 

			float bord = (border.x + border.y - 0.36) * 8;
			bord = saturate(bord*abs(bord)) / 2;
			float deBord = (1 - bord);

			float3 nn2 = UnpackNormal(tex2Dlod(_BumpDetail, float4(IN.uv_BumpDetail, 0, 0)));

			float3 nn =UnpackNormal(tex2Dlod(_Bump, float4((IN.uv_MainTex * frac(_MainTex_TexelSize.z)) +float2( -hold.x , -hold.y ),0,0)));

			nn = (nn + nn2 *0.1* deBord);

			nn = normalize(nn*(1 - bord) + float3(0, 0, bord));

			o.Normal = nn;

			float smudge = tex2D(_Smudge, IN.uv_Smudge).a;

			float gloss = _Glossiness * smudge*deBord + (bord*0.2);

			float4 light = (tex2Dlod(_MainTex, float4(IN.uv_MainTex + (nn.xy)*(_MainTex_TexelSize.xy * (1 + 0.3* smudge)), 0, 0)));

			float4 col = ((c + c * light *(1 - gloss) + light * gloss*0.5))*(1 - bord);

			o.Albedo = col.rgb*deBord*deBord;
			o.Metallic = _Metallic;
			o.Smoothness = gloss;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
