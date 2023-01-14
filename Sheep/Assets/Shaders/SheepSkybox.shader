// This shader fills the mesh shape with a color predefined in the code.
Shader "Skybox/Sheep-Cubemap"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    {
        _Tint("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure("Exposure", Range(0, 8)) = 1.0
        _Rotation("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _Tex("Cubemap   (HDR)", Cube) = "grey" {}
    }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        // Workaround instancing bug
        // https://forum.unity.com/threads/hlsl-or-shadergraph-skybox-shader-with-single-pass-instancing.1234660/
        //ZClip False

        Pass
        {
            // This used to be an HLSL converted shader, but currently HLSL skyboxes ar broken on Quest
            // https://forum.unity.com/threads/hlsl-or-shadergraph-skybox-shader-with-single-pass-instancing.1234660/
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile_instancing
            #define HLSL_WORKAROUND 1

            //HLSL
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"     
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnityCG.cginc"
            #include "General/ShaderUtils.hlsl"


            struct Attributes
            {
                float4 positionOS   : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            samplerCUBE _Tex;
            half4 _Tex_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation;

            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * 3.14159 / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 rotated = RotateAroundYInDegrees(IN.positionOS.xyz, _Rotation);
                //HLSL
                //OUT.positionHCS = TransformObjectToHClip(rotated);
                OUT.positionHCS = UnityObjectToClipPos(rotated);
                OUT.texcoord = IN.positionOS.xyz;
                return OUT;
            }

            // The fragment shader definition.            
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                half4 tex = texCUBE(_Tex, IN.texcoord);
                //HLSL
                //half3 c = DecodeHDREnvironment(tex, _Tex_HDR);
                half3 c = DecodeHDR(tex, _Tex_HDR);

                //HLSL
                /*#ifdef UNITY_COLORSPACE_GAMMA
                    half4 unity_ColorSpaceDouble = half4(2.0, 2.0, 2.0, 2.0);
                #else
                    half4 unity_ColorSpaceDouble = half4(4.59479380, 4.59479380, 4.59479380, 2.0);
                #endif*/
                c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
                c *= _Exposure;

                c = LumaBasedReinhardToneMapping(c);
                c = AdjustAccordingToLUT(c);

                return half4(c, 1);
            }
            ENDCG
        }
    }
}