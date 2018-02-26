Shader "Editor/MeshEditorAssist" {
	Properties {
	 _MainTex("_MainTex", 2D) = "white" {}
	_AtlasTextures("_Textures In Row _ Atlas", float) = 1
	}
	Category {
	Tags { "LightMode"="ForwardBase"}
	 		
	

	
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityLightingCommon.cginc" 
			 #include "Lighting.cginc"
			#include "UnityCG.cginc"
			 #include "AutoLight.cginc"
			#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

			 #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

			#pragma multi_compile  MESH_PREVIEW_LIT MESH_PREVIEW_NORMAL MESH_PREVIEW_VERTCOLOR MESH_PREVIEW_PROJECTION MESH_PREVIEW_SHARP_NORMAL
		

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;

			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 vcol : COLOR0;
				float4 texcoord : TEXCOORD0; // zw will contain projected UV.
				SHADOW_COORDS(1) 
				float3 scenepos : TEXCOORD2;
				float3 viewDir: TEXCOORD3;
				float3 normal: TEXCOORD4;
				float3 snormal: TEXCOORD5; // .w will contain texture number.
				float4 bC : TEXCOORD6; 
				float4 atlasedUV : TEXCOORD7;
				float4 edge : TEXCOORD11;
			};
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float4 _GridDotPosition;
			float _AtlasTextures;

			v2f vert (appdata_full v)  {
				v2f o;
			
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord.xy = v.texcoord;
				o.vcol = v.color;
				o.scenepos.xyz = mul(unity_ObjectToWorld, v.vertex);
				o.edge = v.texcoord1;

				normalAndPositionToUV(v.texcoord2.xyz, o.scenepos.xyz, o.bC, o.texcoord.zw);

				o.viewDir.xyz = WorldSpaceViewDir(v.vertex); 

				o.normal.xyz = UnityObjectToWorldNormal(v.normal.xyz);//v.tangent.xyz;//v.normal;
				o.snormal.xyz = UnityObjectToWorldNormal(v.texcoord2.xyz);  // Sharp Normal
			
				float atlasNumber = v.texcoord.z;//v.tangent.w;
				
				float atY = floor(atlasNumber / _AtlasTextures);
				float atX = atlasNumber - atY*_AtlasTextures;
				float edge = _MainTex_TexelSize.x;

				o.atlasedUV.xy = float2(atX, atY) / _AtlasTextures;				//+edge;
				o.atlasedUV.z = edge;										//(1) / _AtlasTextures - edge * 2;
				o.atlasedUV.w = 1 / _AtlasTextures;

				TRANSFER_SHADOW(o);

				return o;
			}

			float4 frag (v2f i) : COLOR {
				i.viewDir.xyz = normalize(i.viewDir.xyz);
				float dist = length(i.scenepos.xyz - _WorldSpaceCameraPos.xyz);
				
			// Cubics part
				float3 awpos  = abs(i.scenepos.xyz);
				int3 iwpos = awpos;
				float3 smooth = abs(awpos-iwpos-0.5);
				float smoothing = max(smooth.x , max(smooth.y, (smooth.z)));
				int ind = (iwpos.x+iwpos.y+iwpos.z);
				int o = ind*0.5f;
				smoothing = saturate((0.499-smoothing)*512/dist);
				float val =  0.75+ abs(ind - o*2)*0.25*smoothing + 0.125*(1-smoothing);
				  

				float2 border = DetectEdge(i.edge);
				float deEdge = 1 - border.y;
				float deBorder = 1 - border.x;
				i.normal.xyz = i.snormal.xyz*deBorder + i.normal.xyz*border.x;



				float mip = (0.5 *log2(dist*0.5));

				float edge = i.atlasedUV.z*pow(2, mip);

				i.texcoord.zw = (frac(i.texcoord.zw)*(i.atlasedUV.w 
					- edge
					) 
					
					+ i.atlasedUV.xy+ edge*0.5);

				float4 col =  tex2Dlod(_MainTex, float4(i.texcoord.zw,0, mip));   //perfTex  + off
			
				//applyTangentNonNormalized(i.bC, i.normal.xyz, bump.rg);

				//i.normal.xyz = normalize(i.normal.xyz);


				col.rgb = col.rgb*(1 - border.y) + i.vcol.rgb*border.y;

			
				

				float dotprod = dot(i.viewDir.xyz, i.normal.xyz);					 //dot(normal,  i.viewDir.xyz);
				float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*i.normal.xyz);
				float dott = max(0, dot(_WorldSpaceLightPos0, -reflected));


				float shadow = SHADOW_ATTENUATION(i);

				float4 cont = 0;

				float diff = max(0, dot(i.normal.xyz, _WorldSpaceLightPos0.xyz));
			

			

				col.rgb = col.rgb* (_LightColor0.rgb*diff*shadow) + pow(dott, 128)*_LightColor0.rgb;

				col = saturate(col)*val*0.95 + val*0.05;

#if MESH_PREVIEW_VERTCOLOR
				return i.vcol -border.x;
#endif

#if MESH_PREVIEW_PROJECTION
				col.r = frac(i.texcoord.z);
				col.g = frac(i.texcoord.w);
				col.b = 0.1;
#endif

#if MESH_PREVIEW_NORMAL
				col.r = i.normal.x; //yz
				col.g = i.normal.y; //yz
				col.b = i.normal.z; //yz
				col.rgb += 0.5;
#endif

#if MESH_PREVIEW_SHARP_NORMAL
				col.r = i.snormal.x; //yz
				col.g = i.snormal.y; //yz
				col.b = i.snormal.z; //yz
				col.rgb += 0.5;
#endif

				//return i.snormal.w;
				return col;
			}
			ENDCG 
		}

			Pass {
		
				Cull Front 

			CGPROGRAM
		
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityLightingCommon.cginc" 
			 #include "Lighting.cginc"
			#include "UnityCG.cginc"
			 #include "AutoLight.cginc"

			 #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;

			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 diff: COLOR0;
				float4 vcol : COLOR1;
				float4 texcoord : TEXCOORD0;
				   SHADOW_COORDS(1) // put shadows data into TEXCOORD1
				float3 scenepos : TEXCOORD2;
				float3 viewDir: TEXCOORD3;
				float3 normal: TEXCOORD4;
				float3 snormal: TEXCOORD5;
				float4 bC : TEXCOORD6; 
			};
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float4 _GridDotPosition;

			v2f vert (appdata_full v)  {
				v2f o;
			
				o.pos = UnityObjectToClipPos(v.vertex);
				o.texcoord.xy = v.texcoord;
				o.vcol = v.color;
				o.scenepos.xyz = mul(unity_ObjectToWorld, v.vertex);
				

				float rmx = o.scenepos.x;
				float rmy = o.scenepos.z;


                float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                o.diff = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz)) * _LightColor0;


				worldNormal = abs(worldNormal);
				float znorm = saturate((worldNormal.x-worldNormal.z)*55555);
				float xnorm = saturate(((worldNormal.z+worldNormal.y) - worldNormal.x)*55555);
				float ynorm = saturate((worldNormal.y-0.8)*55555);
					
				float x = ((o.scenepos.x)*(xnorm)+(o.scenepos.z)*(1-xnorm))*8;
				float y = (o.scenepos.y)*(1-ynorm)+(o.scenepos.z)*(ynorm)*8;

				o.texcoord.z = x;
				o.texcoord.w = y;

				float dey = 1-ynorm;

				o.bC.w = xnorm*ynorm;
				o.bC.x = o.bC.w + (1-znorm)*dey;
				o.bC.y = dey; 
				o.bC.z = znorm*dey; 
			

				o.viewDir.xyz = WorldSpaceViewDir(v.vertex); 

				o.normal.xyz = UnityObjectToWorldNormal(v.normal.xyz);//v.tangent.xyz;//v.normal;
				o.snormal.xyz = UnityObjectToWorldNormal(v.tangent.xyz);

				TRANSFER_SHADOW(o)

				return o;
			}

			float4 frag (v2f i) : COLOR {

				float dist = length(i.scenepos.xyz - _WorldSpaceCameraPos.xyz);
				//MESH_PREVIEW_LIT MESH_PREVIEW_NORMAL MESH_PREVIEW_VERTCOLOR MESH_PREVIEW_PROJECTION 
				i.scenepos.xyz*=2;
			
				
				float3 awpos  = abs(i.scenepos.xyz);
				int3 iwpos = awpos;
				float3 smooth = abs(awpos-iwpos-0.5);
				float smoothing = max(smooth.x , max(smooth.y, (smooth.z)));
				int ind = (iwpos.x+iwpos.y+iwpos.z);
				int o = ind*0.5f;
				smoothing = saturate((0.499-smoothing)*512/dist);
				float val =  0.5+ abs(ind - o*2)*0.5*smoothing + 0.25*(1-smoothing);

			return val;



			}
			ENDCG 
		}

		  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

	}	
}
}
