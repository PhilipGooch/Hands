#ifndef PAINTERLY_SIMPLE_LIT_INPUT_INCLUDED
#define PAINTERLY_SIMPLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

// USED BY GRASS NO BRAKES SHADER
// COPIED FROM URP SOURCE SimpleLitInput.hlsl
// We need to do this in order to add our own fields into the UnityPerMaterial buffer for SRP batcher

CBUFFER_START(UnityPerMaterial)
half4 _BaseMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Surface;
half _MainNoiseScale;
half _MainNoiseStrength;
half _SecondaryNoiseScale;
half _SecondaryNoiseStrength;
half _NoiseBlend;
half _WindStrength;
half _SecondaryWindStrength;
half _WindSpeed;
half _WindScale;
half _TintTop;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
UNITY_DOTS_INSTANCED_PROP(float, _Cutoff)
UNITY_DOTS_INSTANCED_PROP(float, _Surface)
UNITY_DOTS_INSTANCED_PROP(float, _MainNoiseScale)
UNITY_DOTS_INSTANCED_PROP(float, _MainNoiseStrength)
UNITY_DOTS_INSTANCED_PROP(float, _SecondaryNoiseScale)
UNITY_DOTS_INSTANCED_PROP(float, _SecondaryNoiseStrength)
UNITY_DOTS_INSTANCED_PROP(float, _NoiseBlend)
UNITY_DOTS_INSTANCED_PROP(float, _WindStrength)
UNITY_DOTS_INSTANCED_PROP(float, _SecondaryWindStrength)
UNITY_DOTS_INSTANCED_PROP(float, _WindSpeed)
UNITY_DOTS_INSTANCED_PROP(float, _WindScale)
UNITY_DOTS_INSTANCED_PROP(float, _TintTop)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor					UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata__BaseColor)
#define _SpecColor					UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata__SpecColor)
#define _EmissionColor				UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata__EmissionColor)
#define _Cutoff						UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__Cutoff)
#define _Surface					UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__Surface)
#define _MainNoiseScale				UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__MainNoiseScale)
#define _MainNoiseStrength          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__MainNoiseStrength)
#define _SecondaryNoiseScale        UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__SecondaryNoiseScale)
#define _SecondaryNoiseStrength     UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__SecondaryNoiseStrength)
#define _NoiseBlend					UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__NoiseBlend)
#define _WindStrength				UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__WindStrength)
#define _SecondaryWindStrength		UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__SecondaryWindStrength)
#define _WindSpeed					UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__WindSpeed)
#define _WindScale					UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__WindScale)
#define _TintTop					UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__TintTop)
#endif

TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);

#include "../General/GeneralSimpleLitFunctions.hlsl"

#endif
