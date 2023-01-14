Shader "Unlit/LowQualityWater"
{
    Properties
    {
		_BumpMap("Normal Map", 2D) = "bump" {}
		_Color ("Color", Color) = (0,0.5,1,0.5)
		_Smoothness("Smoothness", Range(0,1)) = 1.0
		_ShadowWobbleAmount("Shadow Wobble Amount", Float) = 0.05
		_ReflectionAmount("Reflection Amount", Range(0,1)) = 0.5
		_NormalStrength("Normal Strength", Float) = 1.0
		_NormalSpeed("Normal Speed", Float) = 0.01
		_NormalScale("Normal Scale", Float) = 10
		_WaterfallColor("Waterfall Color", Color) = (1,1,1,1)
		_WaterfallFoamScale("Waterfall Foam Scale", Float) = 10
		_WaterfallFoamSpeed("Waterfall Foam Speed", Float) = 0.01
		_WaterfallFoamDistance("Waterfall Foam Distance", Float) = 0.2
		_WaterfallFoamAmount("Waterfall Foam Amount", Float) = 1
		_WaterfallFoamStrength("Waterfall Foam Strength", Float) = 1
		_WaterfallFoamNoiseTexture("Foam (R) Noise (G) Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
			Tags { "LightMode" = "UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			//ZWrite On

			Stencil{
				Ref 1
				Comp Always
				Pass Replace
				Fail Keep
			}

            HLSLPROGRAM
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
			//#pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
			#pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA
			//#pragma shader_feature_local _NORMALMAP
			//#pragma shader_feature_local_fragment _EMISSION
			//#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#define BUMP_SCALE_NOT_SUPPORTED 1
			#define _NORMALMAP 1
			#define _SPECULAR_COLOR 1
			#define UNITY_ASSUME_UNIFORM_SCALING 1

			// -------------------------------------
			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			#include "WaterShadingFunctions.hlsl"
			#include "General/ShaderUtils.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
				float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
				float2 lightmapUV : TEXCOORD1;
				float4 tangentOS : TANGENT;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD2;
				float4 normal : TEXCOORD3;
				// x: fogFactor y: steepness z: transparency
				float3 miscData : TEXCOORD4;
				float4 bitangent : TEXCOORD5;
				float4 tangent : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

			CBUFFER_START(UnityPerMaterial)
				half4 _Color;
				half _Smoothness;
				half _ShadowWobbleAmount;
				half _ReflectionAmount;
				half _NormalStrength;
				half _NormalSpeed;
				half _NormalScale;
				half4 _WaterfallColor;
				half _WaterfallFoamScale;
				half _WaterfallFoamSpeed;
				half _WaterfallFoamDistance;
				half _WaterfallFoamAmount;
				half _WaterfallFoamStrength;
				sampler2D _WaterfallFoamNoiseTexture;
			CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInput.positionCS;
				o.worldPos = vertexInput.positionWS;
				o.miscData.x = ComputeFogFactor(vertexInput.positionCS.z);
				half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
				VertexNormalInputs normalInput = GetVertexNormalInputs(v.normal, v.tangentOS);
				o.normal = half4(normalInput.normalWS, viewDirWS.x);
				o.tangent = half4(normalInput.tangentWS, viewDirWS.y);
				o.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
				// Steepness
				o.miscData.y = 1 - saturate(dot(o.normal, half3(0, 1, 0)));
				o.miscData.z = v.color.r;
				o.uv = v.uv;
				OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
				OUTPUT_SH(o.normal.xyz, o.vertexSH);
                return o;
            }

            half4 frag (v2f i, half facing : VFACE) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				facing = step(0.5, facing);

				half4 col = _Color;
				half steepness = i.miscData.y;

				half3 normalTS = WaterNormal(TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), i.uv, _NormalSpeed, _NormalScale, _NormalStrength);
				half3 normal = TransformTangentToWorld(normalTS, half3x3(i.tangent.xyz, i.bitangent.xyz, i.normal.xyz));
				half3 viewDirWS = SafeNormalize(half3(i.normal.w, i.tangent.w, i.bitangent.w));
				normal = lerp(normal, i.normal.xyz, steepness);
				normal = NormalizeNormalPerPixel(normal);

				half3 emission = half3(0, 0, 0);
				half3 bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, half3(0,1,0));

				half reflectionAmount = lerp(0, _ReflectionAmount, facing);

				col += Waterfall(_WaterfallColor, _WaterfallFoamScale, _WaterfallFoamSpeed, steepness, _WaterfallFoamDistance,
					_WaterfallFoamAmount, _WaterfallFoamStrength, _WaterfallFoamNoiseTexture, i.uv);
				col = WaterLightingSimple(col.rgb, col.a, _Smoothness, _ShadowWobbleAmount, i.worldPos,
					normalTS, normal, viewDirWS, bakedGI, emission, i.miscData.x, reflectionAmount, steepness);
				col.a = lerp(0, col.a, i.miscData.z);

				col.rgb = LumaBasedReinhardToneMapping(col.rgb);
				col.rgb = AdjustAccordingToLUT(col.rgb);
				return col;
            }
            ENDHLSL
        }
    }
}
