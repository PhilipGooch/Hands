#ifndef WATER_SHADING_FUNCTIONS_INCLUDED
#define WATER_SHADING_FUNCTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float CalculateSteepness(float3 worldNormal)
{
	return 1 - saturate(dot(worldNormal, half3(0, 1, 0)));
}

float3 VertexWobble(float3 objectPos, float3 objectNormal, float4 objectTangent, float wobbleAmount, float wobbleFrequency, float wobbleDensity)
{
	VertexPositionInputs vertexInput = GetVertexPositionInputs(objectPos);
	float3 worldPos = vertexInput.positionWS;
	VertexNormalInputs normalInput = GetVertexNormalInputs(objectNormal, objectTangent);
	float steepness = CalculateSteepness(normalInput.normalWS);

	float posSum = worldPos.x + worldPos.y + worldPos.z;
	posSum *= wobbleDensity;
	float time = _Time.y * wobbleFrequency;
	float wobble = sin(time + posSum) * wobbleAmount;
	float3 normalWobble = objectNormal * wobble;
	normalWobble *= steepness;
	return objectPos + normalWobble;
}

float2 UVWithDirectionTilingAndOffset(float2 uv, float tiling, float speed)
{
	float movement = _Time.y * speed * tiling;
	return uv * tiling + float2(0, movement);
}

half3 NormalStrength(half3 normal, half strength)
{
	return half3(normal.rg * strength, lerp(1, normal.b, saturate(strength)));
}

half3 WaterNormal(TEXTURE2D_PARAM(BumpMap, sampler_BumpMap), float2 uv, float scrollSpeed, float scale, float strength)
{
	float2 uv1 = UVWithDirectionTilingAndOffset(uv, scale, scrollSpeed);
	float2 uv2 = UVWithDirectionTilingAndOffset(uv, scale * 1.5, -scrollSpeed * 0.25);

	half3 normals = lerp(SampleNormal(uv1, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap)), SampleNormal(uv2, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap)), 0.5);
	normals = normalize(normals);
	return NormalStrength(normals, strength);
}

float PerlinNoiseFromTexture(float2 uv, float scale, sampler2D perlinNoiseTexture)
{
	return tex2D(perlinNoiseTexture, uv * scale).g;
}

half4 UnderwaterColor(float depth, float underwaterSkyDepth, half4 underwaterSilhouetteColor, half4 underwaterSkyColor, half4 sceneColor)
{
	half normalizedDepth = saturate(depth / underwaterSkyDepth);
	half4 color = lerp(underwaterSilhouetteColor, underwaterSkyColor, normalizedDepth);
	color = lerp(sceneColor, half4(color.rgb, 1), color.a);
	return color;
}

float3 Refraction(float2 uv, float scale, float speed, float strength, TEXTURE2D_PARAM(BumpMap, sampler_BumpMap), float3 worldPos, float3x3 tangentMatrix,
	float3 screenPos)
{
	float2 noiseUV = UVWithDirectionTilingAndOffset(uv, scale * 1.5, speed);
	float2 noiseUV2 = UVWithDirectionTilingAndOffset(uv, scale, -speed);
	float3 normal = SampleNormal(noiseUV, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
	float3 normal2 = SampleNormal(noiseUV, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));

	normal = normalize(normal + normal2);
	normal *= strength;
	return screenPos + normal;
}

float WaterDepth(float shallowWaterDepth, float deepWaterTransition, float actualDepth, float3 worldPos, float3 normal, float4 screenPos)
{
	float3 posDiff = _WorldSpaceCameraPos - worldPos;
	posDiff /= screenPos.w;
	posDiff *= actualDepth;
	float3 camPos = _WorldSpaceCameraPos - posDiff;
	float3 offsetPos = worldPos - camPos;
	offsetPos *= normal;
	float depthSum = ((offsetPos.x + offsetPos.y + offsetPos.z) - shallowWaterDepth) / deepWaterTransition;
	depthSum = sqrt(saturate(depthSum));
	return depthSum;
}

float Foam(float2 uv, float foamScale, float foamSpeed, float foamAmount, float foamCutoff, float steepness, 
	sampler2D perlinNoiseTexture, float depth, float3 worldPos, float3 normal, float4 screenPos)
{
	float foamDepth = WaterDepth(0, foamAmount, depth, worldPos, normal, screenPos);
	foamDepth *= foamCutoff;
	foamDepth += steepness * 10;
	float2 noiseUV = UVWithDirectionTilingAndOffset(uv, foamScale, foamSpeed);
	float noise = PerlinNoiseFromTexture(noiseUV, 0.1, perlinNoiseTexture);
	float foam = step(foamDepth, noise);
	return foam;
}

float3 WorldPosFromDepth(float depth, float3 viewDir)
{
	float3 _CameraDirection = -1 * mul(UNITY_MATRIX_M, transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)) [2].xyz);
	float viewDot = dot(_CameraDirection, viewDir);
	float3 normalizedView = viewDir / viewDot;
	return _WorldSpaceCameraPos + normalizedView * depth;
}

float Caustics(float density, float strength, float power, float3 movement, sampler2D causticsTexture, float depth, float3 viewDir)
{
	float3 depthPos = WorldPosFromDepth(depth, viewDir);
	float2 causticPos = float2(depthPos.x, depthPos.z) * density;
	float3 causticsMovement = _Time.y * movement * density;
	const float offset = 0.005;
	float brightness = tex2D(causticsTexture, causticPos + causticsMovement + float2(offset,offset)).r;
	causticPos = float2(depthPos.x, depthPos.z) * density * 0.9;
	causticsMovement = _Time.y * movement * density * -1.1;
	brightness = min(brightness, tex2D(causticsTexture, causticPos + causticsMovement).r);

	brightness = saturate(pow(brightness, power)) * strength;
	brightness *= 1 - step(50, depth);
	return brightness;
}

half4 Waterfall(half4 waterfallColor, half waterfallFoamScale, half waterfallSpeed, half steepness, half foamDistance,
	half foamAmount, half foamStrength, sampler2D foamNoiseTexture, float2 uv)
{
	half distanceMultiplier = saturate(steepness / foamDistance);
	half foamVisibility = distanceMultiplier * (1 - foamAmount);

	const half waterfallYStretch = 0.1;
	float2 stretchedUV = float2(uv.x, uv.y * waterfallYStretch);

	float2 tiledMovement = UVWithDirectionTilingAndOffset(stretchedUV, waterfallFoamScale, waterfallSpeed);
	half foam = tex2D(foamNoiseTexture, tiledMovement).r;
	foam -= foamVisibility;
	foam *= 0.75;

	float2 stretchedTiledMovement = UVWithDirectionTilingAndOffset(stretchedUV, waterfallFoamScale + 5.0f, waterfallSpeed * 2.0f);
	half stretchedFoam = tex2D(foamNoiseTexture, stretchedTiledMovement).r;
	stretchedFoam -= foamVisibility;

	foam += stretchedFoam;
	foam *= foamStrength;

	float2 perlinUV = UVWithDirectionTilingAndOffset(uv, waterfallFoamScale * 25.0f, waterfallSpeed * 2.0f);
	half perlinNoise = tex2D(foamNoiseTexture, perlinUV).g * 0.3;
	foam -= perlinNoise;

	foam = saturate(foam);
	half4 foamColor = waterfallColor * foam * waterfallColor.a *steepness;
	return foamColor;
}

half3 SampleReflections(float3 viewDir, float3 normal, float reflectionAmount)
{
	half3 reflectVector = reflect(-viewDir, normal);
	return GlossyEnvironmentReflection(reflectVector, PerceptualSmoothnessToPerceptualRoughness(reflectionAmount), 1);
}

half4 WaterLighting(half3 albedo, half alpha, half smoothness, float shadowWobbleAmount, float3 worldPos,
	half3 worldNormal, half3 worldView, half3 bakedGI, half3 emission, float fogFactor, float reflectionAmount)
{
	worldNormal = normalize(worldNormal);
	worldView = SafeNormalize(worldView);
	half3 lightWorldPos = worldPos + half3(worldNormal.x, 0, worldNormal.z) * shadowWobbleAmount;
	half4 shadowCoord = TransformWorldToShadowCoord(lightWorldPos);
	Light mainLight = GetMainLight(shadowCoord, lightWorldPos, half4(1, 1, 1, 1));
	bool hideSpecularHighlight = mainLight.shadowAttenuation < 0.99;

	BRDFData brdfData;

	InitializeBRDFData(albedo, 0, half3(0, 0, 0), smoothness, alpha, brdfData);
	BRDFData brdfDataClearCoat = (BRDFData)0;
	half occlusion = 1.0;

	brdfData.perceptualRoughness = lerp(1, brdfData.perceptualRoughness, reflectionAmount);

	MixRealtimeAndBakedGI(mainLight, worldNormal, bakedGI);
	half3 color = GlobalIllumination(brdfData, brdfDataClearCoat, 0,
		bakedGI, occlusion,
		worldNormal, worldView);
	color += LightingPhysicallyBased(brdfData, mainLight, worldNormal, worldView, hideSpecularHighlight);
	color += emission;
	color = MixFogColor(color, unity_FogColor, fogFactor);
	return half4(color, alpha);
}

half4 WaterLightingSimple(half3 albedo, half alpha, half smoothness, float shadowWobbleAmount, float3 worldPos, half3 normalTS,
	half3 worldNormal, half3 worldView, half3 bakedGI, half3 emission, float fogFactor, float reflectionAmount, float steepness)
{
	InputData inputData;
	inputData.positionWS = worldPos;
	inputData.normalWS = worldNormal;
	inputData.viewDirectionWS = worldView;

	half3 lightWorldPos = worldPos + half3(worldNormal.x, 0, worldNormal.z) * shadowWobbleAmount;
	half4 shadowCoord = TransformWorldToShadowCoord(lightWorldPos);
	inputData.shadowCoord = shadowCoord;

	Light mainLight = GetMainLight(shadowCoord, lightWorldPos, half4(1, 1, 1, 1));
	float hideSpecularHighlight = step(0.99, mainLight.shadowAttenuation);

	inputData.fogCoord = fogFactor;
	inputData.vertexLighting = half3(0,0,0);
	inputData.bakedGI = bakedGI;
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(mul(worldPos, UNITY_MATRIX_VP));
	inputData.shadowMask = half4(0, 0, 0, 0);
	half4 specular = half4(1, 1, 1, 1) * hideSpecularHighlight * 2;
	smoothness = exp2(10 * smoothness + 1);
	specular.a = smoothness;

	// Hack to get specular highlights with baked lighting
	// Alternative is to implement a custom UniversalFragmentBlinnPhong and change the main light distance attenuation there
	half previousLightData = unity_LightData.z;
	unity_LightData.z = 1;
	half4 color = UniversalFragmentBlinnPhong(inputData, half4(albedo, alpha), specular, smoothness, emission, alpha, normalTS);
	half3 reflection = half4(SampleReflections(worldView, worldNormal, reflectionAmount),1);
	half NdotV = saturate(dot(worldNormal, worldView) * 0.5);
	half finalReflectionAmount = lerp(0, lerp(1 - NdotV, 0, steepness), reflectionAmount);
	color.rgb = lerp(color.rgb, reflection, finalReflectionAmount);
	return color;


	unity_LightData.z = previousLightData;
	color.rgb = MixFog(color.rgb, inputData.fogCoord);
	return color;
}

#endif