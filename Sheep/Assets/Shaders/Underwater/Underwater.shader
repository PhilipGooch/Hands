Shader "Effects/Underwater"
{
	Properties
	{
		//[MainTexture] _MainTex("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
	#include "UnderwaterCommon.hlsl"

	TEXTURE2D_X(_MainTex);
	float4 _MainTex_TexelSize;
	float _BlurSize;
	float4 _Color;
	float _MaxDistance;
	float _MinDistance;

	float4 BaseTexture(float2 uv)
	{
		return SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv);
	}

	float GetRelativeDistance(float2 uv)
	{
		float eyeDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams) - _MinDistance;
		return saturate(eyeDepth / (_MaxDistance - _MinDistance));
	}

	float4 Blur(float2 uv)
	{
		float2 a1 = float2(-1, -1);
		float2 a2 = float2(1, -1);
		float2 a3 = float2(-1, 1);
		float2 a4 = float2(1, 1);

		float2 texel = _MainTex_TexelSize.xy * _BlurSize * GetRelativeDistance(uv);
		float4 col = SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv);
		col += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv + a1 * texel);
		col += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv + a2 * texel);
		col += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv + a3 * texel);
		col += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, uv + a4 * texel);
		return col / 5.0;
	}

	float4 Fog(float2 uv)
	{
		float colorAmount = GetRelativeDistance(uv);
		return lerp(BaseTexture(uv), _Color, min(colorAmount, _Color.a));
	}

	float4 blurFrag(v2f i) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(i);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		#if UNITY_UV_STARTS_AT_TOP 
			i.uv = 1.0 - i.uv;
		#endif
		if (i.underwater)
		{
			return Blur(i.uv);
		}
		return BaseTexture(i.uv);
	}

		float4 fogFrag(v2f i) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(i);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
		#if UNITY_UV_STARTS_AT_TOP 
			i.uv = 1.0 - i.uv;
		#endif
		if (i.underwater)
		{
			return Fog(i.uv);
		}
		return BaseTexture(i.uv);
	}

	ENDHLSL


	SubShader
	{
		Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		ZTest Always ZWrite Off Cull Off
		LOD 100
		//ZTest Always ZWrite Off Cull Off

		Pass
		{
			HLSLPROGRAM
				#pragma vertex underwaterVert
				#pragma fragment blurFrag
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
				#pragma vertex underwaterVert
				#pragma fragment fogFrag
			ENDHLSL
		}
	}
}
