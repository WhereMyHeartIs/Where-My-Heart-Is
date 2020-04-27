Shader "Watercolour/Main"
{
	Properties {
		_Color ("Tint Color 1", Color) = (1,1,1,1)
		_Color2 ("Tint Color 2", Color) = (1,1,1,1)
		_InkCol ("Ink Color", Color) = (1,1,1,1)
		
		_BlotchTex ("Blotches (RGB)", 2D) = "white" {}
		_DetailTex ("Detail (RGB)", 2D) = "white" {}
		_PaperTex ("Paper (RGB)", 2D) = "white" {}
		_RampTex ("Ramp (RGB)", 2D) = "white" {}
		
		_TintScale ("Tint Scale", Range(2,8)) = 4
		_PaperStrength ("Paper Strength", Range(0,1)) = 1
		_BlotchMulti ("Blotch Multiply", Range(0,16)) = 4
		_BlotchSub ("Blotch Subtract", Range(0,8)) = 2
		_BlotchWorldScale ("Blotch World Scale", Range(0, 1)) = 0.2// testing world uv indexing

		[PowerSlider(2)] _AttenExponent ("Attenuation Exponent", Range(0, 2)) = 1

		// _Dissolve ("Dissolve", int) = 0
		// _LightAttenBias ("Inverse Light Factor", Range(0,30)) = 25
	}
	SubShader {
		Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
		LOD 200

		CGPROGRAM
		#pragma surface surf BlinnPhong noforwardadd nolightmap vertex:vert finalcolor:colour
		#pragma target 3.0

		#pragma multi_compile_local __ DISSOLVE DISSOLVE_MANUAL

		sampler2D _BlotchTex, _DetailTex, _PaperTex;
		sampler2D _RampTex;
		float4 _RampTex_TexelSize;
		half _BlotchMulti;
		half _BlotchSub;
		half _TintScale;
		half _PaperStrength;
		half _BlotchWorldScale;// testing world uv indexing
		fixed4 _Color, _Color2, _InkCol;
		half _AttenExponent;

		// int _Dissolve;
		float3 _ViewDir;
		float _ManualDissolve;

		float _LightAttenBias;

		struct Input {
			float2 uv_BlotchTex : TEXCOORD0;
			float2 uv_DetailTex : TEXCOORD1;
			float2 uv_PaperTex : TEXCOORD2;
			float2 uv_RampTex : TEXCOORD3;
			float3 worldNormal;
			//#if DISSOLVE
			float3 worldPos;
			//#endif
			// float3 lightDir;
			float4 lightColour;
			float lightAtten;
		};

		UNITY_INSTANCING_BUFFER_START(Props)
		// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)
		
		fixed4 screen (fixed4 colA, fixed4 colB) {
			// fixed4 white = fixed4(1, 1, 1, 1);
			return 1 - (1 - colA) * (1 - colB);
		}

		fixed4 softlight (fixed4 colA, fixed4 colB) {
			return (1 - 2 * colB) * pow(colA, 2) + 2 * colB * colA;
		}

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

			// float3 lightDir = float3(0, 0, 0);
			float4 lightColour = float4(0, 0, 0, 0);
			float lightAtten = 0;
			int lights = 4;

			// _WorldSpaceLightPos0 // direction of dir light, w = 0 if dir light, else 1
			// UnityWorldSpaceLightDir() // _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;
			
			for(int i = 0; i < 4; i++) {
				float3 lightPos = float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]);
				if(lightPos[0] == 0 && lightPos[1] == 0 && lightPos[2] == 0) {
					lights--;
					continue;
				}
				float3 vertToLight = lightPos - worldPos;
				// lightDir += normalize(vertToLight);

				// float normDist = unity_4LightAtten0[i] * length(vertToLight, vertToLight);
				float normMag = unity_4LightAtten0[i] * unity_4LightAtten0[i] * dot(vertToLight, vertToLight); // normDist^2
				// lightAtten += 1 / (1 + _LightAttenBias * normDist * normDist);
				float atten = 1.0 / (1.0 + _LightAttenBias * normMag) * saturate((1 - sqrt(normMag)) * 5.0);

				// lightColour.w += 1.0 / (1.0 + _LightAttenBias * normMag) * saturate((1 - sqrt(normMag)) * 5.0);
				// lightColour.xyz += unity_LightColor[i].xyz * unity_LightColor[i].a;
				lightAtten += atten;
				lightColour += unity_LightColor[i] * atten;
			}
			// o.lightDir = lightDir / lights;

			// o.lightColour = float4(lightColour.xyz / lights, saturate(lightColour.w));
			o.lightColour = lightColour / lights;
			o.lightAtten = saturate(lightAtten);

			// o.lightDir = normalize(lightDir);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			#if DISSOLVE
				// if(_Dissolve == 1) {
					float camDist = distance(IN.worldPos, _WorldSpaceCameraPos + float3(_ViewDir.x, max(0, _ViewDir.y), _ViewDir.z));
					float isVisible = tex2D(_DetailTex, IN.uv_DetailTex).r * 0.999 - exp(-camDist);
					clip(isVisible); // TODO: set by alpha instead?
				// }
			#elif DISSOLVE_MANUAL
				float isVisible = tex2D(_DetailTex, IN.uv_DetailTex).r * 0.999 - _ManualDissolve;
				clip(isVisible);
			#endif

			float2 worldUV = float2(((IN.worldPos.x + IN.worldPos.y) * _BlotchWorldScale) % 1, ((IN.worldPos.z + IN.worldPos.y) * _BlotchWorldScale) % 1);


			//fixed c = tex2D(_BlotchTex, IN.uv_BlotchTex).r;
			fixed c = tex2D (_BlotchTex, worldUV).r;
			c *= _BlotchMulti;
			c -= _BlotchSub;			
			//c *= tex2D(_DetailTex, IN.uv_DetailTex).r;
			c *= tex2D (_DetailTex, worldUV).r;

			// float lightAtten = IN.lightColour.w;
			float lightAtten = IN.lightAtten;
			// float f = (1 - dot(IN.worldNormal, IN.lightDir)) * lightAtten;
			float f = lightAtten;
			f = pow(f, _AttenExponent);

			c = saturate(c * .3 + f);
			c = tex2D (_RampTex, half2(1 - c, 0)).r;
			c = saturate(c);


			//fixed4 tint = tex2D (_BlotchTex, IN.uv_BlotchTex / _TintScale);	
			fixed4 tint = tex2D (_BlotchTex, worldUV / _TintScale);
			tint = lerp(_Color, _Color2, tint.r)  + IN.lightColour * f;
			
			fixed4 ink = screen(_InkCol, fixed4(c, c, c, 1));

			o.Albedo = lerp(ink * tint, softlight(tex2D (_PaperTex, IN.uv_PaperTex), ink * tint), _PaperStrength);
		}

		void colour(Input IN, SurfaceOutput o, inout fixed4 color) {
			// color += IN.lightColour;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
