// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Painter_Experimental/AtlasedStandard" {
	 Properties {
        _MainTex("Base texture", 2D) = "white" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _OcclusionMap("Occlusion", 2D) = "white" {}

		[Toggle(UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
        _AtlasTextures("_Textures In Row _ Atlas", float) = 1
			
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "UnityLightingCommon.cginc" 
            #include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"
			#pragma multi_compile  ___ UV_ATLASED



            	sampler2D _MainTex;
				sampler2D _BumpMap;
				sampler2D _OcclusionMap;
				float _AtlasTextures;
				float4 _MainTex_TexelSize;

            // exactly the same as in previous shader
            struct v2f {
                float3 worldPos : TEXCOORD0;
                float3 tspace0 : TEXCOORD1;
                float3 tspace1 : TEXCOORD2;
                float3 tspace2 : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 pos : SV_POSITION;
                float4 vcol : COLOR0;
                SHADOW_COORDS(5)
#if defined(UV_ATLASED)
					float4 atlasedUV : TEXCOORD6;
#endif
            };
            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                half3 wNormal = UnityObjectToWorldNormal(v.normal);
                half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
                o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
                o.uv = v.texcoord.xy;

				o.vcol = v.color;

#if defined(UV_ATLASED)
				vert_atlasedTexture(_AtlasTextures,   v.texcoord.z,  _MainTex_TexelSize.x,  o.atlasedUV);
#endif

                TRANSFER_SHADOW(o);

              

                return o;
            }

         
        
            float4 frag (v2f i) : SV_Target
            {
        
#if UV_ATLASED
			i.uv = (frac(i.uv)*(i.atlasedUV.w) + i.atlasedUV.xy);
#endif

                // same as from previous shader...
                float3 tnormal = UnpackNormal(tex2D(_BumpMap, i.uv));
                float3 worldNormal;
                worldNormal.x = dot(i.tspace0, tnormal);
                worldNormal.y = dot(i.tspace1, tnormal);
                worldNormal.z = dot(i.tspace2, tnormal);
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 worldRefl = reflect(-worldViewDir, worldNormal);

                float4 col = tex2D(_MainTex, i.uv);

                float4 occlude = tex2D(_OcclusionMap, i.uv);
                        
                float3 diff = 
                max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz)) 
                * _LightColor0 + _LightColor0*0.5*occlude;

                col.rgb *= diff;

                return col;
            }
            ENDCG
        }
	
    }
		FallBack "Diffuse"
}
