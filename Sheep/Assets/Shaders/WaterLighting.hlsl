void DirectSpecular_half(half3 albedo, half alpha, half smoothness, float shadowWobbleAmount, float3 worldPos,
	half3 worldNormal, half3 worldView, half3 bakedGI, half3 emission, float fogFactor, float reflectionAmount, out half4 Out)
{
#if defined(SHADERGRAPH_PREVIEW)
	Out = 0;
#else
	worldNormal = normalize(worldNormal);
	worldView = SafeNormalize(worldView);
	half3 lightWorldPos = worldPos + half3(worldNormal.x, 0, worldNormal.z) * shadowWobbleAmount;
	half4 shadowCoord = TransformWorldToShadowCoord(lightWorldPos);
	Light mainLight = GetMainLight(shadowCoord, lightWorldPos, half4(1,1,1,1));
	// This forces the water to have specular highlights when using baked lighting
	mainLight.distanceAttenuation = 1.0;
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
	Out = half4(color, alpha);
#endif
}