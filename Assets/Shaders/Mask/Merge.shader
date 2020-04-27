Shader "Mask/Merge"
{
	Properties
	{
		_MainTex ("Real", 2D) = "white" {} // Real 

		_Intensity("Intensity", float) = 2

		[Header(Boil)]
		_HatchTex ("Hatch Texture", 2D) = "white" {}
		_HatchSize ("Size", Int) = 20
		_HatchSpeed ("Speed", Int) = 5
		_Distortion ("Distortion Amount", Range(1, 1000)) = 1000
		_NoiseSpeed ("Distortion Rate", Range(1, 500)) = 200
		_DepthOutlineColour ("Outline Color", Color) = (0.1, 0, 0.1, 1)
		_NormalMult ("Normal Outline Multiplier", Range(0, 4)) = 0.4
		_NormalBias ("Normal Outline Bias", Range(1, 4)) = 4
		_DepthMult ("Depth Outline Multiplier", Range(0, 4)) = 1
		_DepthBias ("Depth Outline Bias", Range(1, 4)) = 1.6

		[Header(Wave)]
		_WaveDistance ("Distance from player", float) = 0
		_WaveTrail ("Length of the trail", Range(0,5)) = 1
		_WaveColour ("Colour", Color) = (1, 1, 1, 1)

		[Header(Fog)]
		_FogExponent("Fog distance exponent", float) = 4
		_FogColor("Fog color", color) = (1, 1, 1, 1)

		_Background ("Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue" = "Transparent" }

		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		// Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
			#pragma exclude_renderers gles
			#pragma vertex vert_img
			#pragma fragment frag

			#pragma multi_compile __ MASK
			#pragma multi_compile __ OUTLINE
			#pragma multi_compile __ BOIL
			#pragma multi_compile __ WAVE
			#pragma multi_compile __ BIRD
			#pragma multi_compile __ FOG
			// #pragma multi_compile __ BLOOM

			#include "UnityCG.cginc"
			// #include "../GaussianBlur.cginc"

			sampler2D _Mask;
			sampler2D _MainTex; // _Real
			sampler2D _Heart;

			sampler2D _GlowMap;
			float4 _GlowMap_TexelSize;
			// sampler2D _CameraDepthTexture;
			// float _Radius;
			float _Intensity;
			// float4 _Filter;

			sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;
			float4 _CameraDepthNormalsTexture_TexelSize;
			static const float4x2 dirs = { 0, 1, 1, 0, 0, -1, -1, 0 };

			float4 _DepthOutlineColour;
			float _NormalMult, _NormalBias;
			float _DepthMult, _DepthBias;

			sampler2D _HatchTex;
			int _HatchSize, _HatchSpeed;
			float _Distortion, _NoiseSpeed;

			float _WaveDistance;
			float _WaveTrail;
			float4 _WaveColour;

			sampler2D _BirdMask;
			sampler2D _Background;

			float _FogExponent;
			float4 _FogColor;

			void Compare(inout float depthOutline, inout float normalOutline, float baseDepth, float3 baseNormal, float2 uv, float2 offset) {
				//read neighbor pixel
				float4 neighbour = tex2D(_CameraDepthNormalsTexture, uv + _CameraDepthNormalsTexture_TexelSize.xy * offset);
				float3 neighborNormal;
				float neighborDepth;
				DecodeDepthNormal(neighbour, neighborDepth, neighborNormal);
				neighborDepth *= _ProjectionParams.z;

				depthOutline += baseDepth - neighborDepth;

				float3 normalDifference = baseNormal - neighborNormal;
				normalDifference = normalDifference.r + normalDifference.g + normalDifference.b;
				normalOutline += normalDifference;
			}

			fixed4 alphaBlend(fixed4 dst, fixed4 src) {
				fixed4 result = fixed4(0, 0, 0, 0);
				result.a = src.a + dst.a * (1 - src.a);
				if (result.a != 0) result.rgb = (src.a + dst.rgb * dst.a * (1 - src.a)) / result.a;
				return result;
			}

			bool rgbEquals(fixed4 a, fixed4 b){
				// return a.r == b.r && a.g == b.g && a.b == b.b;
				if(abs(a.r - b.r) > .1) return false;
				if(abs(a.g - b.g) > .1) return false;
				if(abs(a.b - b.b) > .1) return false;
				return true;
			}

			float2 modUV(float2 uv, float row, float col, int size) {
				row = row % size;
				col = col % size;
				return (uv / size) + float2(row/size, col/size);
			}

			fixed4 frag (v2f_img i) : SV_Target {
				// return tex2D(_BirdMask, i.uv);
				float4 output;
				#if MASK
					float mask = tex2D(_Mask, i.uv).r;
					output = mask > .5 ? mask * tex2D(_Heart, i.uv) + (1 - mask) * tex2D(_MainTex, i.uv) : tex2D(_MainTex, i.uv);
					//output = mask * tex2D(_Heart, i.uv) + (1 - mask) * tex2D(_MainTex, i.uv);
				#else
					output = tex2D(_MainTex, i.uv);
				#endif

				#if OUTLINE
					float4 glow = tex2D(_GlowMap, i.uv);
					// return glow;
					if(glow.a == 0) {
						int NumberOfIterations = 3;
						
						//split texel size into smaller words
						float TX_x = _GlowMap_TexelSize.x;
						float TX_y = _GlowMap_TexelSize.y;
						
						//and a final intensity that increments based on surrounding intensities.
						float4 ColorIntensityInRadius = float4(0, 0, 0, 0);
						
						for(int k = 0; k < NumberOfIterations; k++) {
							for(int j = 0; j < NumberOfIterations; j++) {
								//increase our output color by the pixels in the area
								ColorIntensityInRadius += tex2D(_GlowMap, i.uv.xy + 
								float2((k - NumberOfIterations / 2) * TX_x, (j - NumberOfIterations / 2) * TX_y));
							}
						}

						#if MASK
							if(mask > .5 && ColorIntensityInRadius.a > 0) return ColorIntensityInRadius * _Intensity; 
						#endif

						output += ColorIntensityInRadius * _Intensity;
					}
				#endif

				#if BOIL
					float texIndex = floor(_Time.y * _HatchSpeed % 9);
					float row = 1 + texIndex % (_HatchSize - 1);
					float col = floor(texIndex / _HatchSize);
					
					float4 depthNormal = tex2D(_CameraDepthNormalsTexture, i.uv);

					float3 normal;
					float boilDepth;
					DecodeDepthNormal(depthNormal, boilDepth, normal);
					boilDepth *= _ProjectionParams.z;

					float depthDifference = 0;
					float normalDifference = 0;

					for(int j = 0; j < 4; j++) Compare(depthDifference, normalDifference, boilDepth, normal, i.uv, dirs[j]);

					depthDifference *= _DepthMult;
					depthDifference = saturate(depthDifference);
					depthDifference = pow(depthDifference, _DepthBias);

					normalDifference *= _NormalMult;
					normalDifference = saturate(normalDifference);
					normalDifference = pow(normalDifference, _NormalBias);

					float outline = normalDifference + depthDifference * 2;
					//float4 color = lerp(output, _DepthOutlineColour, outline);
					float4 color = lerp(output, _DepthOutlineColour, outline);
					if (outline > 0.1)
					{
						float4 hatchColor = tex2D(_HatchTex, modUV(i.uv * _HatchSize, row, col, _HatchSize));
						color = lerp(color, output, 1 - hatchColor.r);// subtract back to non-outline based on brightness of hatch
						color = hatchColor;
					}

					#if MASK
					color = output;
					#endif
					output = color;
					/*if (outline > 0.8)
					{
						color = _DepthOutlineColour;
					}
					//output = color;

					if(!rgbEquals(output, color)) { 
						float4 preOutline = color * tex2D(_HatchTex, modUV(i.uv * _HatchSize, row, col, _HatchSize));
						float4 hatchColor = tex2D(_HatchTex, modUV(i.uv * _HatchSize, row, col, _HatchSize));
						

						#if MASK
						if(mask > .5) preOutline = 0; // TODO: heart world depth normal texture lookup;
						#endif

						color = lerp(output, _DepthOutlineColour, outline);
						//output = float4(output.r + color.r * hatchColor.r, output.g + color.g * hatchColor.r, output.b + color.b * hatchColor.r, output.a);
						//output += preOutline;
					}*/
				#endif

				#if WAVE
					float waveDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)) * _ProjectionParams.z;

					if(waveDepth < _ProjectionParams.z) {
						float waveFront = step(waveDepth, _WaveDistance);
						float waveTrail = smoothstep(_WaveDistance - _WaveTrail, _WaveDistance, waveDepth);
						float wave = waveFront * waveTrail;

						output = lerp(output, _WaveColour, wave);
					}
				#endif

				// #if BIRD
				// 	float4 birdMask =  tex2D(_BirdMask, i.uv);
				// 	if(birdMask.r > 0 || birdMask.g > 0 || birdMask.b > 0) {
					// 		#if MASK
					// 			if(mask > .5)  return 1 - float4(tex2D(_Background, i.uv + float2(_Time.x, _Time.x)).rgb, birdMask.b);
					// 		#endif
					// 		return float4(tex2D(_Background, i.uv + float2(_Time.x, _Time.x)).rgb, birdMask.b);
				// 	}
				// #endif

				#if FOG
					float fogStrength = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
					fogStrength = 1 - fogStrength;
					fogStrength = saturate(pow(fogStrength, _FogExponent));
					output = (1 - fogStrength) * output + fogStrength * _FogColor;
				#endif

				return output;
			}
			
			ENDCG
		}
	}
}
