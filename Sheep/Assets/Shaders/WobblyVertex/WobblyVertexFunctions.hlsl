#ifndef WOBBLY_VERTEX_FUNCTIONS_INCLUDED
#define WOBBLY_VERTEX_FUNCTIONS_INCLUDED

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float2 uvLM : TEXCOORD1;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float3 WobbleDeform(float3 position, half wobbleSpeed, half wobbleStrength, half wobbleDensity, half3 wobbleDirection, half wobbleModifier)
{
    float coordinate = position.x + position.y + position.z;
    coordinate *= wobbleDensity;
    float wobbliness = _Time.y * wobbleSpeed + coordinate;
    half3 wobbleMovement = sin(wobbliness) * wobbleStrength * wobbleModifier * wobbleDirection;
    return position + wobbleMovement;
}

#endif