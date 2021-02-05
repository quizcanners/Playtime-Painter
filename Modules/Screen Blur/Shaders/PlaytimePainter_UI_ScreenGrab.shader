Shader "Playtime Painter/UI/ScreenGrab/Display"
{
  Properties
  {
    [PerRendererData]
     _MainTex("Sprite Texture", 2D) = "white" {}
      _Color("Tint", Color) = (1,1,1,1)

      [Toggle(ALPHA_MASK)] _UseMask("Use Alpha Mask", Float) = 0

  }

    SubShader
      {
        Tags
        {
          "Queue" = "Transparent"
          "IgnoreProjector" = "True"
          "RenderType" = "Transparent"
          "PreviewType" = "Plane"
          "CanUseSpriteAtlas" = "True"
        }

     

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
          Name "Default"
          CGPROGRAM
          #pragma vertex vert
          #pragma fragment frag
          #pragma shader_feature ___ ALPHA_MASK

          #include "UnityCG.cginc"
          #include "UnityUI.cginc"

          struct appdata_t
          {
            float4 vertex   : POSITION;
            half4 color    : COLOR;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
          };

          struct v2f
          {
            float4 vertex   : SV_POSITION;
            fixed4 color : COLOR;
            float2 texcoord  : TEXCOORD0;
            float4 worldPosition : TEXCOORD1;
            float4 screenPos : 	TEXCOORD2;
            UNITY_VERTEX_OUTPUT_STEREO
          };

          sampler2D _MainTex;
          sampler2D _qcPp_Global_Screen_Read;
          fixed4 _Color;
          float4 _TextureSampleAdd;
          float4 _MainTex_ST;

          v2f vert(appdata_t v)
          {
            v2f OUT;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            OUT.worldPosition = v.vertex;
            OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

            OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

            OUT.screenPos = ComputeScreenPos(OUT.vertex);

            OUT.color = v.color * _Color;

            return OUT;
          }

          fixed4 frag(v2f IN) : SV_Target {

            float2 screenPos = IN.screenPos.xy / IN.screenPos.w;

            fixed4 color = tex2Dlod(_qcPp_Global_Screen_Read, float4(screenPos , 0, 0));
             
            color.a = 1;

            color *= IN.color;

            #if ALPHA_MASK
              color *= tex2D(_MainTex, IN.texcoord);
            #endif
       
            return color;
          }
        ENDCG
        }
      }

      Fallback "Legacy Shaders/Transparent/VertexLit"
}