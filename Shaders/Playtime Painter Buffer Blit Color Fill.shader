Shader "Playtime Painter/Buffer Blit/Color Fill" 
{
	Properties{	}
	
	Category{
		

		ColorMask RGBA
		Cull Back
		ZTest off
		ZWrite off


		SubShader{

			Tags{
				"RenderPipeline" = "UniversalPipeline"
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"LightMode" = "UniversalForward"
			}

			Pass{

				HLSLPROGRAM
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x
				#pragma target 2.0

				#pragma vertex vert
				#pragma fragment frag

				#include "PlaytimePainter cg.cginc"

				struct v2f {
					float4 pos : POSITION;
				};

				v2f vert(appdata_brush_qc v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					return o;
				}

				float4 frag(v2f i) : COLOR{
					return _qcPp_brushColor;
				}
				ENDHLSL
			}
		}

		SubShader{
		
			Tags{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
			}
		
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				
				#include "PlaytimePainter cg.cginc"

				struct v2f {
					float4 pos : POSITION;
				};

				v2f vert(appdata_brush_qc v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);   
					return o;
				}

				float4 frag(v2f i) : COLOR{
					return _qcPp_brushColor;
				}
				ENDCG
			}
		}
	}
}