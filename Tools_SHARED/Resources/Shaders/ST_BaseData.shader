// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Standart/ST_BaseData" {
	Properties {
	 [NoScaleOffset] _MainTex    ("_MainTex", 2D) = "white" {}
	 [NoScaleOffset] _Bump    ("_Bump", 2D) = "white" {}
	 [NoScaleOffset] _BumpRefl    ("_BumpRefl", 2D) = "white" {}
	 _val0 ("Test", Range (1,100))=8
	_anim("anim frame 1, 2, portion, damageUV", Vector) = (0,0,0,0)
	}
  Category {
	Tags {  "Queue"= "Geometry"
	 		"IgnoreProjector"="True" 
	 		"RenderType"="Opaque" 
			 "LightMode"="ForwardBase"
			}
	 		

	ColorMask RGB
	Cull Back
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			
			uniform sampler2D _MainTex;
			uniform sampler2D _Bump;
			uniform sampler2D _BumpRefl;

			uniform sampler2D _LightMap;
			uniform sampler2D _LightMapBIG;
			float4 _Directional;
			float4 _Glob;

			float4 _de;
			//float _LdeX;
			//float _LdeY;
			float _LWSize;

			float4 _anim;
			uniform sampler2D _Damage;

			//float _deX;
			//float _deY;
			float _WSize;



				float _TdeX;
				float _TdeY;


				float _val0;



			struct v2f {
				float4 pos : POSITION;
				float4 texcoord : TEXCOORD0;  // xy - Main texture UV ; zw - UV from world Pos   
				float4 vcol : COLOR1;  // rgb for border, a - for shadow
				float4 damage: COLOR2;
				float4 viewDir: TEXCOORD1; // w - distance
				float3 normal: TEXCOORD2;
				float3 snormal: TEXCOORD3;

				// zw - Light texture UV
				float4 rendMapUV : TEXCOORD4; // xy - small, zw - big


				float3 dir_H: TEXCOORD5;     // xy - Light Texture Displacement z - height, w - Directional By Tangent
				

			
			};
		

			v2f vert (appdata_full v)  {
				v2f o;
				
				o.damage = tex2Dlod(_Damage, float4(_anim.w, v.tangent.a, 0, 0));

                o.pos = UnityObjectToClipPos(v.vertex);    // Position on the screen
				o.texcoord.xy = v.texcoord;
				o.vcol = v.color;
				
				float3 worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;

				o.rendMapUV.zw = 0.51 + (worldPos.xz -  float2(_de.z,_de.w))/_LWSize ;

				o.rendMapUV.x = (worldPos.x-_de.x)/_WSize+0.5;
				o.rendMapUV.y = (worldPos.z-_de.y)/_WSize+0.5;


				o.dir_H.z = worldPos.y/128;
				
	

				float3 worldNormal = UnityObjectToWorldNormal(v.tangent.xyz);
       
                o.dir_H.x = worldNormal.x/_WSize*64;
				o.dir_H.y = worldNormal.z/_WSize*64;

				worldNormal = abs(worldNormal);
					float xnorm = saturate(((worldNormal.z+worldNormal.y) - worldNormal.x)*5555);
					float ynorm = saturate((worldNormal.y-0.8)*5555);
					
				o.texcoord.z =((o.rendMapUV.x+_TdeX)*(xnorm)+(o.rendMapUV.y+_TdeY)*(1-xnorm))*8;
				o.texcoord.w =(worldPos.y/_WSize*8)*(1-ynorm)+(o.rendMapUV.y+_TdeY)*(ynorm)*8;


				o.viewDir.xyz = WorldSpaceViewDir(v.vertex); //UnityObjectToWorldNormal(normalize(ObjSpaceViewDir(v.vertex)));

				float3 diff = _WorldSpaceCameraPos - worldPos;
				o.viewDir.w =  length(diff)/_WSize/8; 


				o.normal.xyz = UnityObjectToWorldNormal(v.tangent.xyz);//v.tangent.xyz;//v.normal;
				o.snormal.xyz = UnityObjectToWorldNormal(v.normal.xyz);

			
                return o;
			}



			float4 frag (v2f i) : COLOR	{
			
				float4 col = tex2Dlod(_MainTex, float4(i.texcoord.xy,0,0)); //+nn.ba/_val0

				float4 nn = tex2D(_Bump, i.texcoord.zw*32)*(1-col.a) + tex2D(_BumpRefl, i.texcoord.zw*8)*col.a;

				nn.rg -= 0.5;

				float4 vcol = max(0, i.vcol - 0.965);
				i.vcol.a = min(1, vcol.a * 28);
				float deLine =  1 - i.vcol.a; // NEW
				nn = nn*deLine; // NEW
				nn.ba += i.vcol.a;
				col = col*deLine + i.vcol.a*i.vcol;

				float allof = vcol.r+vcol.g+vcol.b;
				float border = min(1,(allof)*(28)+ i.vcol.a); //MODIFIED vcol for smooth: +i.vcol.a
				float deBorder = (1 - border);
	
				nn.ba *= col.a;
				
				i.viewDir.xyz = normalize(i.viewDir.xyz);

				i.normal = deBorder*i.normal + i.snormal*border; 

					i.normal.xz+=nn.rg;
					i.normal.xyz = normalize(i.normal.xyz);

				float dotprod = dot(i.normal.xyz, i.viewDir.xyz);
				float3 reflected =normalize(i.viewDir.xyz - 2*(dotprod)*i.normal.xyz); 

				i.dir_H.xy*=deBorder;

			//	reflected.xz = reflected.xz *nn.b;

				float refdist = reflected.x*reflected.x + reflected.z*reflected.z;
				reflected.xz *= refdist;

				float diff = max(0, dot(i.normal.xyz, _WorldSpaceLightPos0.xyz));


				float4 ln =  tex2Dlod (_LightMap, 	 float4(i.rendMapUV.xy + i.dir_H.xy,0,0) );

				float4 bigL= tex2Dlod (_LightMapBIG, float4(i.rendMapUV.zw+ (nn.rg)/256 
				 ,0,0));  

				float dist = saturate(i.viewDir.w - max(0, i.dir_H.z / 32));

				float4 bigRefl = tex2Dlod(_LightMapBIG, float4(i.rendMapUV.zw - reflected.xz / 16, 0, 0));

			

				float4 refl=  
				tex2Dlod (_LightMap, float4(i.rendMapUV.xy - reflected.xz,0,0) )*4*(nn.a) 
				+ 
				bigRefl*(2-nn.a+dist*8) 
				;
				
		

				_Glob *= max(1 - min(1,bigL.a - i.damage.g),0); // MODIFIED




				float Hshadow = 1-min(saturate(ln.a*8 -i.dir_H.z) + i.vcol.a // MODIFIED remove vcol for smooth
					
					,1); 


				float lightFade = 1-saturate(i.dir_H.z/4*i.normal.y-2 + i.damage.b*4); // MOdified

				//float up = saturate((-reflected.y - 0.1*(2-nn.a))*(1+3*nn.a));  
				float up = saturate((-reflected.y - 0.2*(1.5-nn.a))*(0.5+3*nn.a)); 

				col.rgb =   col.rgb
				 * (_LightColor0.rgb*Hshadow*diff + (ln.rgb + bigL.rgb)*lightFade + _Glob.rgb )

				+ (
				refl.rgb*lightFade
				+ (unity_AmbientSky.rgb)*((1-dotprod))
					*up
					//*(1+nn.a)
					///2
				) *(nn.b*(1-i.damage.a)+i.damage.a)*(1- min(1,bigRefl.a)) // MODIFIED
				;


				float3 flow = (col.gbr + col.brg);
				flow *= flow;
				col.rgb += flow*0.02;

				col.rgb = (unity_AmbientGround.rgb * (dist) +col.rgb*(1-dist))*_Directional.a;


					
				return col; 
				
					
			}
			ENDCG 
		}
	}	
}
}
