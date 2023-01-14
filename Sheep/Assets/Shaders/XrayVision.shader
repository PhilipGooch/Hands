Shader "Universal Render Pipeline/XRay Vision"
{
    Properties
    {
        _Fresnel("Fresnel", Float) = 1
        _FresnelPower("Fresnel Power", Float) = 1
        _Color("Color", Color) = (0,0,1,1)
        [IntRange]_StencilRef("Stencil Ref", Range(0,255)) = 8
        [IntRange]_StencilComp("Stencil Comp", Range(0,8)) = 6
        [IntRange]_ZTest("ZTest", Range(0,6)) = 0
        [Toggle(VERTEX_ALPHA)] _VertexColorAlpha("Vertex Color Alpha", Float) = 0
        [IntRange]_Cull("Cull", Range(0,2)) = 2
        [HideInInspector]_AlphaMultiplier("Alpha Mult", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest [_ZTest]
            ZWrite Off
            Cull [_Cull]

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature VERTEX_ALPHA

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 normal : NORMAL;
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Fresnel;
            float _FresnelPower;
            float4 _Color;
            float _AlphaMultiplier;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = mul(UNITY_MATRIX_MVP, (v.vertex));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz);
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 col = 1.0 - saturate(pow(dot(i.normal, i.viewDir) * _Fresnel, _FresnelPower));
                #if VERTEX_ALPHA
                    col.a = min(col.a * _AlphaMultiplier, 1.0 - i.color.r);
                #endif
                return col * _Color;
            }
            ENDHLSL
        }
    }
}
