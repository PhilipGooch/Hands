#ifndef NBG_SHADER_UTILS_INCLUDED
#define NBG_SHADER_UTILS_INCLUDED	

	half2 CheapAsChipsTriplanar(half3 worldPos, half3 worldNormal)
	{
		half3 absoluteNormal = abs(worldNormal);
		/*
		// BLACK MAGIC WAY OF SNAPPING NORMALS
		float3 hugeNormal = pow(absoluteNormal, 160);
		float hugeNormalLength = hugeNormal.x + hugeNormal.y + hugeNormal.z;
		float3 unitHugeNormal = hugeNormal / hugeNormalLength;
		unitHugeNormal = step(unitHugeNormal, 512);

		float3 scaledWorldPos = worldPos / 1024;*/

		
		// ALTERNATIVE TO BLACK MAGIC FOR SNAPPING NORMALS
		    half3 snappedNormal = half3(0, 0, 1);

			if (absoluteNormal.x > absoluteNormal.y && absoluteNormal.x > absoluteNormal.z) {
				snappedNormal = half3(1, 0, 0);
			}
			else if (absoluteNormal.y > absoluteNormal.x && absoluteNormal.y > absoluteNormal.z) {
				snappedNormal = half3(0, 1, 0);
			}

		/*
		// STEP SNAPPING NORMALS
		float maxNormalValue = max(max(absoluteNormal.x, absoluteNormal.y), absoluteNormal.z);
		float3 snappedNormal = float3(step(absoluteNormal.x, maxNormalValue), step(absoluteNormal.y, maxNormalValue), step(absoluteNormal.z, maxNormalValue));
		*/

		half2 uv = worldPos.xy * snappedNormal.z + worldPos.yz * snappedNormal.x + worldPos.xz * snappedNormal.y;
		return uv;
	}

	float _SheepTonemapperGamma = 2.0;

	half3 LumaBasedReinhardToneMapping(half3 color)
	{
		half3 luma = dot(color.xyz, float3(0.2126, 0.7152, 0.0722));
		float toneMapped = luma / (1.0 + luma);
		color.xyz *= toneMapped / luma;
		half gamma = 1.0 / _SheepTonemapperGamma;
		color.xyz = pow(color.xyz, gamma);
		return color;
	}

	#define LUT_WIDTH 1024.0
	#define LUT_HEIGHT 32.0
	#define LUT_CELLS 32.0

	#ifdef HLSL_WORKAROUND
		UNITY_DECLARE_TEX2D(_SheepLUT);
	#else
		// Use these lines when all shaders are hlsl
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		TEXTURE2D(_SheepLUT);
	#endif

	SamplerState sampler_LinearClamp;
	half _LUTContribution;

	// LUT that samples the texture twice and blends based on the input blue color
	half3 AdjustAccordingToLUT(half3 color)
	{
		color = saturate(color);
		#ifdef HLSL_WORKAROUND
			// Current workaround to support CG shaders
			float3 uvw = color;
			float3 scaleOffset = float3(1.0 / LUT_WIDTH, 1.0 / LUT_HEIGHT, LUT_HEIGHT - 1.0);
			uvw.z *= scaleOffset.z;
			float shift = floor(uvw.z);
			uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
			uvw.x += shift * scaleOffset.y;
			uvw.xyz = lerp(
				_SheepLUT.SampleLevel(sampler_LinearClamp, uvw.xy, 0.0).rgb,
				_SheepLUT.SampleLevel(sampler_LinearClamp, uvw.xy + float2(scaleOffset.y, 0.0), 0.0).rgb,
				uvw.z - shift
			);
			float3 gradedCol = uvw;
		#else
			// Use this line once all shaders are HLSL
			float3 gradedCol = float3(ApplyLut2D(TEXTURE2D_ARGS(_SheepLUT, sampler_LinearClamp), color, float3(1.0 / LUT_WIDTH, 1.0 / LUT_HEIGHT, LUT_HEIGHT - 1.0)));
		#endif

		return lerp(color, gradedCol, _LUTContribution);
	}
#endif