Shader "Playtime Painter/Buffer Blit/Source Texture" 
{
	Properties
	{
	}
	
	Category{
		
		ColorMask RGBA
		Cull Off
		ZTest off
		ZWrite off

		SubShader
		{

			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
			}

			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "PlaytimePainter cg.cginc"


				struct v2f 
				{
					float4 pos : POSITION;
				//	float2 texcoord : TEXCOORD0;  
					float4 screenPos : TEXCOORD0; 
				};

				v2f vert(appdata_full v) 
				{
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);    // Position on the screen
				//	o.texcoord.xy = v.texcoord;
					o.screenPos = ComputeScreenPos(o.pos); 

					return o;
				}

				float4 frag(v2f i) : COLOR
				{
					float2 screenUV = i.screenPos.xy / i.screenPos.w; 
					float4 col = tex2Dlod(_qcPp_DestBuffer, float4(screenUV, 0, 0));
					return col;

				}
				ENDCG
			}
		}
	}
}
