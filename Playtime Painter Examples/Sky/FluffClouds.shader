Shader "Skybox/FluffClouds" {
	Properties {
		[NoScaleOffset]_MainTex ("Base (RGB)", 2D) = "white" { }
		[NoScaleOffset]_FluffMask ("FluffMask (RGB)", 2D) = "white" { }
		_test("Test", Range(0,1)) = 0
	}

	Category {
		Tags { 
		"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"Queue" = "Geometry"
		
		}
	 		 
		ColorMask RGB
		Cull Off
		ZWrite Off
	
		SubShader {
			Pass {
		
				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				//#pragma multi_compile  ___ WATER_FOAM

				#include "Assets/Tools/Playtime Painter/Shaders/quizcanners_cg.cginc"

				sampler2D _MainTex;
				sampler2D _FluffMask;
				float4 _Off;
				float4 _SunDirection;
				float4 _Directional;
				float _test;
			

				struct appdata_t {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 pos : POSITION;
			
					float4 viewDir : TEXCOORD1;
					float h : TEXCOORD2;
					float3 wpos : TEXCOORD3;
					float3 skyvDir : TEXCOORD4;
				};
			
				float4 _MainTex_ST;

				v2f vert (appdata_t v)
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.wpos = v.vertex;//mul(unity_ObjectToWorld, v.vertex).xyz;
					o.skyvDir =  _SunDirection.xyz * 1000;// WorldSpaceViewDir(mul(unity_WorldToObject, _SunDirection.xyz * 60000)).xyz;
					o.wpos = normalize(o.wpos);
					o.viewDir.xyz=WorldSpaceViewDir(v.vertex);
					o.viewDir.a = _WorldSpaceCameraPos.y;
					o.h = v.vertex.y;
					return o;
				}

				float4 frag (v2f i) : COLOR {

					/*
					#if WATER_FOAM
					float3 viewDir = normalize(i.viewDir.xyz);
					float3 projectedWpos;
					float3 waterNormal = SAMPLE_WATER_NORMAL(viewDir, projectedWpos);
					#endif*/


					float vda = i.viewDir.a;
					float dvda = 1 - i.viewDir.a/1024;
					float2 v = i.viewDir.xz / (i.viewDir.y - vda*0.1)*dvda;
					v += _Off.xy;

					float2 sunvv = i.skyvDir.xz / (i.skyvDir.y - vda*0.1)*dvda;
					sunvv += _Off.xy;

					float size=tex2D(_MainTex, v/64).a;

					float _OffsetX = _Time.x / 9;

					float2 off = float2(_OffsetX, -_OffsetX) ;
					float distortion=tex2D(_MainTex, (v)/34+ off).a/16;
					off.x+=_OffsetX*0.65;
					float4 colDet=tex2D(_MainTex, (v)/3 + off);
					off.y-=_OffsetX*0.7;
					colDet.a *= (tex2D(_MainTex, float4(v + off - distortion,0,0)).a )+0.5;
	
					off.x+=_OffsetX*0.55;
					float4 col = tex2D(_MainTex, (v )/(16) + (off - distortion)/8 );
					float sunblock =  tex2D(_MainTex, (v + sunvv * 15) / (256) + (off - distortion) / 8).a;

					off.y-=_OffsetX/2;

					col.a*=(size+colDet.a*(1-size));

					col.a*=8;
				
					float2 v2=v; 
					v2.x-=_OffsetX/(3);

					float thickness = (2.5 - saturate(col.a) * 2);

					col.rgb*=(_LightColor0.rgb*thickness + unity_AmbientSky.rgb*(1 - thickness));    // Make thin clouds brighter

					col.a-=(	tex2D(_FluffMask, v / (2 )).a*(tex2D(_FluffMask, v2 + col.a / 16).a
						+ tex2D(_MainTex, (v2 )*(5)).a/4));

					i.viewDir.xyz = normalize(i.viewDir.xyz);

					col.a=max(0,(col.a / 12 - distortion)); // IMPORTANT

					float ecvator = i.h * 16 + vda / 512;

					float alpha = saturate(ecvator); 
					float ca = col.a;
					float dca = 1 - ca;

					col.rgb = (unity_AmbientSky.rgb*
					(dca) + col.rgb*ca).rgb;

					float3 dist = _SunDirection.xyz - i.wpos; 
					float fdist = 1 - (dist.x*dist.x + dist.y*dist.y + dist.z*dist.z);
					float radial = fdist;

					radial = max(0, radial - sunblock);
					radial *= radial;

					float power = (1-ca) * 2048;
					float sun = pow(max(0,fdist), power)*4;
					fdist = max(0, fdist*(1.5+col.a*3) - 1)*max(0,(0.5-ca));
					fdist *= fdist*80;

					col.rgb += ((colDet.rgb*col.a)*fdist*alpha + radial*max(0, -i.viewDir.y-0.25))*_Directional.rgb ;

					col.rgb+=sun;

					float deAlpha = 1 - alpha;




					#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
					float3 fogColor = unity_FogColor.rgb;
					#else
					float3 fogColor = unity_AmbientEquator.rgb;
					#endif

				/*
#if WATER_FOAM
					
					float4 wcol = 0;
					
					//wcol.rgb = fogColor;
		

					float showWater = max(0, viewDir.y * 4);

					Terrain_Water_AndLight(wcol, WORLD_POS_TO_TERRAIN_UV_3D(projectedWpos), 1, 1, waterNormal, viewDir.xyz, 1, 0);

					col = wcol * showWater + col * (1- showWater);

					BleedAndBrightness(col, 1);

					alpha = saturate(alpha + showWater);

					deAlpha = 1 - alpha;
					//return showFog;
#endif*/


					BleedAndBrightness(col, 1);

					col.rgb = col.rgb*alpha + fogColor.rgb*deAlpha;

			
					return col;
				}
				ENDCG 
			}
		}	
	}
}