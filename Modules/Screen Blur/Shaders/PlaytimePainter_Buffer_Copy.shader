Shader "Playtime Painter/Buffer Blit/Copy"
{
    Properties{
      _MainTex("Main Texture", 2D) = "black"{}
    }

    SubShader{

        Pass {

          ColorMask RGBA
          Cull Off
          ZTest Always
          ZWrite Off
        Fog { Mode off }

           CGPROGRAM
          #pragma vertex vert_img
          #pragma fragment frag
          #pragma fragmentoption ARB_precision_hint_fastest
          #include "UnityCG.cginc"

            uniform sampler2D _MainTex;

            float4 frag(v2f_img i) : COLOR {
                return tex2D(_MainTex, i.uv);
            }


          ENDCG
        }
    }
      FallBack off
}