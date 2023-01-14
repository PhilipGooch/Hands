Shader "Unlit/ColorBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay+1" "RenderPipeline" = "UniversalPipeline"}
		ZTest Always ZWrite On Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_instancing

            //#include "UnityCG.cginc"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
				uint vertexID : SV_VertexID;
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
            };

			//UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);

            v2f vert (appdata v)
            {
                v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				//o.vertex = GetQuadVertexPosition(v.vertexID);
				//o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
                //o.vertex = TransformObjectToHClip(v.vertex.xyz);
                //o.vertex = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)));
				//o.vertex = v.vertex;//TransformObjectToHClip(v.vertex.xyz);
				//o.vertex = GetQuadVertexPosition(v.vertexID);
				//o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				o.uv = v.uv;
                return o;
            }
			float4 _Color;

            float4 frag (v2f i) : SV_Target
            {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				//fixed4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
				//col = lerp(col, _Color, _Color.a);

                return _Color;
            }
            ENDHLSL
        }
    }
}
