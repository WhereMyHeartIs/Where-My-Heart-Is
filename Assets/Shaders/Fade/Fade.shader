﻿Shader "Screen/Fade"
{
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Alpha ("Fade Amount", float) = 1
	}
	SubShader {
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Alpha;

			fixed4 frag (v2f_img i) : SV_Target
			{
				return lerp(tex2D(_MainTex, i.uv), fixed4(1, 1, 1, 1), _Alpha);
			}
			ENDCG
		}
	}
}