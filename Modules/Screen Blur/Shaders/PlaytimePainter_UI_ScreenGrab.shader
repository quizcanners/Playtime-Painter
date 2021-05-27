Shader "Playtime Painter/UI/ScreenGrab/Display"
{
  Properties
  {
    [PerRendererData]
     _MainTex("Sprite Texture", 2D) = "white" {}
      _Color("Tint", Color) = (1,1,1,1)
      [Toggle(ALPHA_MASK)] _UseMask("Use Alpha Mask", Float) = 0

    [KeywordEnum(SCREEN_SHOT, BLURRED_SCREEN)] _Target("Screen Grab Data", Float) = 0
           
    _StencilComp("Stencil Comparison", Float) = 8
    _Stencil("Stencil ID", Float) = 0
    _StencilOp("Stencil Operation", Float) = 0
    _StencilWriteMask("Stencil Write Mask", Float) = 255
    _StencilReadMask("Stencil Read Mask", Float) = 255
    _ColorMask("Color Mask", Float) = 15

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

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }
     

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
          Name "Default"
          CGPROGRAM
          #pragma vertex vert
          #pragma fragment frag
          #pragma shader_feature ___ ALPHA_MASK
          #pragma shader_feature _SCREEN_SHOT  _BLURRED_SCREEN 
          #pragma multi_compile __ _qcPp_FEED_MOUSE_POSITION
        

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

          #if _SCREEN_SHOT  
            sampler2D _qcPp_Global_Screen_Read;
          #else
            sampler2D _qcPp_Global_Screen_Effect;
          #endif
          fixed4 _Color;
          float4 _TextureSampleAdd;
          float4 _MainTex_ST;
          float4 _qcPp_MousePosition;

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

            fixed4 color = tex2Dlod(
                #if _SCREEN_SHOT  
                    _qcPp_Global_Screen_Read
                #else //_BLURRED_SCREEN
                    _qcPp_Global_Screen_Effect
                #endif
                
                , float4(screenPos , 0, 0));
             
            #if _qcPp_FEED_MOUSE_POSITION

                half2 fromMouse = (screenPos - _qcPp_MousePosition.xy);

                fromMouse.x *= _qcPp_MousePosition.w;

                float lenM = length(fromMouse);

                color.a = smoothstep(max(0, 0.99 - (lenM) * 0.9), 1, IN.color.a);

                color.rgb *= IN.color.rgb;

            #else
                color.a = 1;
                color *= IN.color;
            #endif

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