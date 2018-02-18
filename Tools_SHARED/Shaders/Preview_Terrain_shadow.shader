Shader "Editor/br_TerrainPreview_Shadow" {
	SubShader
     {
         Pass
         {
             Name "ShadowCaster"
             Tags { "LightMode" = "ShadowCaster" }
           
             Fog { Mode Off }
             ZWrite On ZTest Less Cull Off
             Offset 1, 1
             
             CGPROGRAM
 
             #pragma vertex vert
             #pragma fragment frag
             #pragma multi_compile_shadowcaster
             #pragma fragmentoption ARB_precision_hint_fastest

        #include "VertexDataProcessInclude.cginc"
             #include "UnityCG.cginc"
 
             struct v2f
             { 
                 V2F_SHADOW_CASTER;

                float3 viewDir : TEXCOORD2; 
                float3 wpos : TEXCOORD3;
                float3 tc_Control : TEXCOORD4;
                float3 normal : TEXCOORD7;
                float2 texcoord : TEXCOORD8;
             };
           
             v2f vert(appdata_base v)
             {
                 v2f o;
                 TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)


                 float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                float height = tex2Dlod(_mergeTerrainHeight, float4(o.tc_Control.xy, 0, 0)).a;
                float2 ts = _mergeTerrainHeight_TexelSize.xy;
                float up    = tex2Dlod(_mergeTerrainHeight, float4(o.tc_Control.x,        o.tc_Control.y+ts.y , 0, 0)).a;
                float right = tex2Dlod(_mergeTerrainHeight, float4(o.tc_Control.x+ts.x,   o.tc_Control.y , 0, 0)).a;

                worldPos.y = _mergeTeraPosition.y + height*_mergeTerrainScale.y;
                v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz, v.vertex.w));

                o.tc_Control.xyz = (worldPos.xyz - _mergeTeraPosition.xyz) / _mergeTerrainScale.xyz;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = worldPos;
                o.viewDir.xyz= (WorldSpaceViewDir(v.vertex));
            
                o.texcoord = v.texcoord;
                TRANSFER_SHADOW(o);

                float3 worldNormal =  float3(height-right, 0.1, height-up);//UnityObjectToWorldNormal(v.normal);

                o.normal =  normalize(worldNormal);

                return o;


                 return o;
             }
           
             float4 frag(v2f i) : COLOR
             {
                 SHADOW_CASTER_FRAGMENT(i)
             }
 
             ENDCG
          }
      }
      FallBack Off
}
