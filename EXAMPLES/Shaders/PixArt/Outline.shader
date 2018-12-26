
Shader "Playtime Painter/Pixel Art/Outline" {
	Properties{
		 _MainTex("Tex", 2D) = "white" {}
	}

	Category{
		Tags{ 
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType"="Opaque"
			"LightMode" = "ForwardBase"
		}

		ColorMask RGB
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"

				sampler2D _MainTex;
				float4 _MainTex_TexelSize;

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;  // xy - Main texture UV ; zw - UV from world Pos   
				};

				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);    // Position on the screen
					o.texcoord.xy = v.texcoord;

					return o;
				}

				float4 frag(v2f i) : COLOR{

					float2 up = i.texcoord.xy*_MainTex_TexelSize.z;
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

					hold *= _MainTex_TexelSize.z/6.5;

					bord = abs(bord);

					float4 difff = abs(contact - c);
					float xsame = saturate((0.3 - (difff.r + difff.g + difff.b + difff.a)) * 165800);
					difff = abs(contact2 - c);
					float ysame = saturate((0.3 - (difff.r + difff.g + difff.b + difff.a)) * 165800);
					difff = abs(contact3 - c);
					float ddiff = saturate(((difff.r + difff.g + difff.b + difff.a) - 0.3) * 165800);

					ddiff = saturate(ddiff*xsame*ysame* 165800);
					float DeDiff = (1 - ddiff);

					bord.x *= ((1 - xsame)*DeDiff + ddiff);
					bord.y *= ((1 - ysame)*DeDiff + ddiff);

					float XaboveY = saturate((bord.x - bord.y) * 165800);
					float YaboveX = 1 - XaboveY;

					float XaYd = XaboveY*ddiff;
					float YaYX = YaboveX*ddiff;

					hold.x *= (XaYd + xsame*DeDiff);
					hold.y *= (YaYX + ysame*DeDiff);

					bord.x *= (YaYX + XaboveY*DeDiff);
					bord.y *= (XaYd + YaboveX*DeDiff);

					float wid = (bord.x + bord.y - 0.32) * 8;
					wid = saturate(wid*abs(wid));

					float2 off = (i.texcoord.xy - up);
					float2 sat = (abs(off) * 1024);
					float2 pixuv = up + off*saturate(sat - 14);

					float4 col = tex2Dlod(_MainTex, float4(pixuv, 0, 0))*(1-wid);
					col.a+= wid;

					return  col;
				}
				ENDCG
			}
		}
	}
}
