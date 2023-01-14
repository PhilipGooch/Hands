Shader "Unlit/BlobShadow"
{
    Properties
    {
		_ShadowSize("Shadow Size", Vector) = (0.5,0.5,0,0)
		_Falloff("Falloff", Float) = 2.0
		_Strength("Strength", Range(0.0,1.0)) = 1.0
		_Roundness("Roundness", Float) = 0.0
    }
        SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
			Tags { "LightMode" = "UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha
			ZTest LEqual

            HLSLPROGRAM
			// -------------------------------------

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				half3 vertexOS : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

			CBUFFER_START(UnityPerMaterial)
				half2 _ShadowSize;
				half _Falloff;
				half _Strength;
				half _Roundness;
			CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInput.positionCS;
				o.vertexOS = v.vertex.xyz;
                return o;
            }

			float BoxSDF(half2 pos, half2 extents)
			{
				half2 dist = abs(pos) - extents;
				return length(max(dist, 0)) + min(max(dist.x, dist.y), 0.0);
			}

            half4 frag (v2f i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				half2 coord = i.vertexOS.xy;
				half sdfVal = saturate(BoxSDF(coord, _ShadowSize - _Roundness) - _Roundness) * _Falloff;
				half distance = (1 - sdfVal) * _Strength;

				return half4(0,0,0,distance);
            }
            ENDHLSL
        }
    }
}