// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Painter_Experimental/PixArt/BumpedOutline" {
	Properties {
		[NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Bump ("Bump (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
			_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
		LOD 200
		
		CGPROGRAM


		#pragma surface surf Standard fullforwardshadows alpha
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Bump;
		float4 _MainTex_TexelSize;

		float _Glossiness;
		float _Metallic;

		struct Input {
			float2 uv_MainTex;

		};

		

		void surf (Input IN, inout SurfaceOutputStandard o) {

		float2 up=IN.uv_MainTex*_MainTex_TexelSize.z;
		float2 border=up;
		up=floor(up);
		border=border-up-0.5;
		float2 hold=border*2;
		hold*= _MainTex_TexelSize.x;
		up=(up+0.5)* _MainTex_TexelSize.x;
		
		float4 c = tex2Dlod (_MainTex, float4(up,0,0));

		
			
		float4 contact  = tex2Dlod (_MainTex, float4(up+float2(hold.x,0),0,0));
		float4 contact2 = tex2Dlod (_MainTex, float4(up+float2(0,hold.y),0,0));
		float4 contact3 = tex2Dlod (_MainTex, float4(up+float2(hold.x,hold.y),0,0));
	
		hold*= _MainTex_TexelSize.z /6.5;
		
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


o.Normal=UnpackNormal(tex2Dlod(_Bump, float4(IN.uv_MainTex* _MainTex_TexelSize.z +float2( -hold.x , -hold.y ),0,0)));



	float wid=(border.x+border.y-0.36)*8;
	wid = saturate(wid*abs(wid))/2;

		c.rgb = ((//contact.rgb
			 tex2D(_MainTex, IN.uv_MainTex).rgb
		

			
			)*wid+c.rgb*(1-wid));
		o.Albedo = c.rgb-wid/2; 
		o.Alpha = min(1,(c.a-0.01)*100);

		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
	

		}
		ENDCG
	} 
//Fallback "Custom/vlit"
}
