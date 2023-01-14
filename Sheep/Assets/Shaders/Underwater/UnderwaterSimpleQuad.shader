Shader "Effects/UnderwaterSimpleQuad"
{
    SubShader
    {

        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" }
        ZTest Always ZWrite Off Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #include "UnderwaterCommon.hlsl"

            #pragma vertex underwaterVert
            #pragma fragment simpleColorFrag

            float4 _Color;

            float4 simpleColorFrag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if (i.underwater)
                {
                    return _Color;
                }
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
