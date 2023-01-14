#ifndef UNDERWATER_COMMON_INCLUDED
#define UNDERWATER_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

float2 _EyeState;

struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	float2 uv : TEXCOORD0;
	float underwater : TEXCOORD1;
	float4 vertex : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

v2f underwaterVert(appdata v)
{
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	o.vertex = float4(v.vertex.xyz, 1.0);
	o.uv = v.uv;
	if (_ProjectionParams.x < 0)
	{
		o.uv = float2(o.uv.x, 1 - o.uv.y);
	}
	o.underwater = 0;
	if (unity_StereoEyeIndex == 0 && _EyeState.x > 0)
	{
		o.underwater = 1;
	}
	else if (unity_StereoEyeIndex == 1 && _EyeState.y > 0)
	{
		o.underwater = 1;
	}

	return o;
}

#endif