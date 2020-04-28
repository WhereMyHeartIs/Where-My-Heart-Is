Shader "Screen/Ripple"
{
    Properties
    {
		[Header(Displacement textures)]
		_MainTex("Texture", 2D) = "white" {}
		_BiasTex ("Bias Texture", 2D) = "white" {}

		[Header(Displacement values)]
		_Offset("Offset", float) = 0
		_Width ("Displacement Width", float) = 0
		_Strength ("Displacement Strength", float) = 0

		[Header(Pattern offset and scale)]
		_ScaleX ("Pattern scale X", float) = 1
		_ScaleY ("Pattern scale Y", float) = 1
		_TranslationX ("Patern Translation X", float) = 0
		_TranslationY ("Patern Translation Y", float) = 0
	}
		SubShader
		{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				float _Offset;
				float _Width;
				float _Strength;
				sampler2D _BiasTex;
				float _ScaleX;
				float _ScaleY;
				float _TranslationX;
				float _TranslationY;

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				v2f vert (appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				sampler2D _MainTex;


				float heartEquation(float sinT, float cosT)
				{
					return 1 - sinT;
					//return length(float2(cosT, sinT));
					//return (sinT * sqrt(abs(cosT))) / ((sinT)+(7 / 5)) - 1 * sinT + 2;
					//return 2 - 2 * cosT + cosT * (sqrt(abs(sinT))/(cosT + 1.4));
				}

				fixed4 frag(v2f i) : SV_Target
				{
					/*float uDif = abs(i.uv.x - 0.5);
					float vDif = abs(i.uv.y - 0.5);
					float distance = sqrt((uDif * uDif) + (vDif * vDif));*/
					float x = (1/_ScaleX) * (i.uv.x - 0.5 - _TranslationX);
					float y = (1 / _ScaleY) * (i.uv.y - 0.5 - _TranslationY - _Offset);
					//float distance = length((i.uv.xy - float2(0.5, 0.5) - float2(_TranslationX, _TranslationY));
					float distance = length(float2(x, y));

					float2 direction = normalize(i.uv.xy - float2(0.5, 0.5) - float2(_TranslationX, _TranslationY + _Offset));
					//float angle = atan2(i.uv.x, i.uv.y);
					float heartOffset = _Offset * heartEquation(direction.y, direction.x);//heart eqn here as function of i.uv.xy
					float diffFromOffset = abs(heartOffset - distance);
					//float diffFromOffset = _Offset - sqrDist;

					fixed4 col;

					if (diffFromOffset > _Width)
					{
						col = tex2D(_MainTex, i.uv + i.uv * (_Width/diffFromOffset) * _Strength * tex2D(_BiasTex, i.uv));
					}
					else
					{
						col = tex2D(_MainTex, i.uv);
					}

					//col = float4 (heartOffset * direction.x, heartOffset * direction.y, 0, 1);

					//col = float4(((_Width / diffFromOffset) * _Strength), 0, 0, 1);
					return col;
				}

				/*
				float heartEquation(float angle)
				{
					float sinT = sin(angle);
					float cosT = cos(angle);
					return sinT * sqrt(cos(angle))
				}*/
				ENDCG
			}
		}
}
