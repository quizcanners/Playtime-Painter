Shader "PlaytimePainter/Smoke_VolumeShadowed" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}	
		[NoScaleOffset]_BakedShadow_VOL("Baked Shadow Volume (RGB)", 2D) = "grey" {}
		_Thickness("Thickness", Range(0,20)) = 0.0
		VOLUME_H_SLICES("Baked Shadow Slices", Vector) = (0,0,0,0)
		VOLUME_POSITION_N_SIZE("Baked Shadow Position & Size", Vector) = (0,0,0,0)

		l0pos("Point light 0 world scene position", Vector) = (0,0,0,0)
		l0col("Point light 0 Color", Vector) = (0,0,0,0)
		l1pos("Point light 1 world scene position", Vector) = (0,0,0,0)
		l1col("Point light 1 Color", Vector) = (0,0,0,0)
		l2pos("Point light 2 world scene position", Vector) = (0,0,0,0)
		l2col("Point light 2 Color", Vector) = (0,0,0,0)

	}
		Category{
		Tags{
			//"Queue" = "Transparent"
		//"IgnoreProjector" = "True"
		//"RenderType" = "Transparent"
		//"LightMode" = "ForwardBase"

			"Queue" = "AlphaTest"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"

		//"Queue" = "AlphaTest+50"
		//"IgnoreProjector" = "True" 
		//"RenderType" = "TransparentCutout"
	}


		//	ColorMask RGB
			Cull Off//Back
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

		SubShader{

			     

		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile_fwdbase
#pragma target 3.0
#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED

	uniform sampler2D _MainTex;
	uniform sampler2D _BakedShadow_VOL;

	float4 l0pos;
	float4 l0col;
	float4 l1pos;
	float4 l1col;
	float4 l2pos;
	float4 l2col;
	
	float _Thickness;

	float4  _MainTex_ST;
	float4 _BakedShadows_VOL_TexelSize;
	float4 VOLUME_H_SLICES;
	float4 VOLUME_POSITION_N_SIZE;

	struct v2f {
		float4 pos : SV_POSITION;
		float3 worldPos : TEXCOORD0;
		float3 normal : TEXCOORD1;
		float2 texcoord : TEXCOORD2;
		SHADOW_COORDS(3)
		float3 viewDir: TEXCOORD4;
		UNITY_FOG_COORDS(5)

	};


	v2f vert(appdata_full v) {
		v2f o;

		o.normal.xyz = UnityObjectToWorldNormal(v.normal);
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

		o.texcoord = v.texcoord.xy;//TRANSFORM_TEX(v.texcoord.xy, _MainTex_ATL); ;
	
		UNITY_TRANSFER_FOG(o, o.pos);
		TRANSFER_SHADOW(o);

		return o;
	}



	float4 frag(v2f i) : COLOR{

		float2 off = i.texcoord - 0.5;
		off *= off;

		i.viewDir.xyz = normalize(i.viewDir.xyz);

		float distance = (10 - max(0,10 - length(_WorldSpaceCameraPos - i.worldPos)))*0.1;


		float alpha = max(0, (1- (off.x+ off.y) * 4)*abs(dot(i.viewDir.xyz, i.normal.xyz)))*distance;

		float2 tc = TRANSFORM_TEX(i.texcoord, _MainTex);
		float4 col = tex2D(_MainTex, tc);

		col.a *= alpha;

		

		float ambientBlock =col.a;

		float3 normal = -i.viewDir.xyz;

		float3 thickness = normal * _Thickness * ambientBlock;

	

		float4 bake = 1- SampleVolume(_BakedShadow_VOL, i.worldPos,  VOLUME_POSITION_N_SIZE,  VOLUME_H_SLICES, thickness);

		float4 bake2 = 1-  SampleVolume(_BakedShadow_VOL, i.worldPos, VOLUME_POSITION_N_SIZE, VOLUME_H_SLICES, -thickness);


		float4 directBake = (saturate((bake - 0.5) * 2) + saturate((bake2 - 0.5) * 2))*(ambientBlock);

		bake =(bake + bake2) * 0.5;

		

		float3 scatter = 0;
		float3 directLight = 0;

		// Point Lights

		PointLightTransparent(scatter, directLight, i.worldPos.xyz - l0pos.xyz,
			 i.viewDir.xyz, ambientBlock, bake.r, directBake.r, l0col);

		PointLightTransparent(scatter, directLight, i.worldPos.xyz - l1pos.xyz,
			 i.viewDir.xyz, ambientBlock, bake.g, directBake.g, l1col);

		PointLightTransparent(scatter, directLight, i.worldPos.xyz - l2pos.xyz,
			 i.viewDir.xyz, ambientBlock, bake.b, directBake.b, l2col);

		scatter *= (1 - bake.a);

		DirectionalLightTransparent(scatter, directLight,
			//SHADOW_ATTENUATION(i)
			directBake.a
			,
			
			normal, i.viewDir, ambientBlock, bake.a);


		col.rgb *= (directLight + scatter);

		//col.rgb += (glossLight)*directBake.a;


#if	MODIFY_BRIGHTNESS
		col.rgb *= _lightControl.a;
#endif

#if COLOR_BLEED
		float3 mix = col.gbr + col.brg;
		col.rgb += mix * mix*_lightControl.r;
#endif

		UNITY_APPLY_FOG(i.fogCoord, col);

		return col;

	}
		ENDCG

	}
		//UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	Fallback "Legacy Shaders/Transparent/VertexLit"
	//FallBack "Diffuse"
	}

}

