Shader "Unlit/HighQualityWater"
{
    Properties
    {
        _BumpMap("Normal Map", 2D) = "bump" {}
		_Smoothness("Smoothness", Range(0,1)) = 1.0
		_ShadowWobbleAmount("Shadow Wobble Amount", Float) = 0.05
		_ReflectionAmount("Reflection Amount", Range(0,1)) = 0.5
		_NormalStrength("Normal Strength", Float) = 1.0
		_NormalSpeed("Normal Speed", Float) = 0.01
		_NormalScale("Normal Scale", Float) = 10
		_RefractionSpeed("Refraction Speed", Float) = 1.0
		_RefractionScale("Refraction Scale", Float) = 1.0
		_RefractionStrength("Refraction Strength", Float) = 0.1
		_ShallowWaterDepth("Shallow Water Depth", Float) = 0.0
		_DeepWaterTransition("Deep Water Transition Distance", Float) = 5.0
		_ShallowWaterColor("Shallow Water Color", Color) = (0,0,1,1)
		_DeepWaterColor("Deep Water Color", Color) = (0,0,1,1)
		_FoamColor("Foam Color", Color) = (1,1,1,1)
		_FoamScale("Foam Scale", Float) = 1
		_FoamSpeed("Foam Speed", Float) = 1
		_FoamAmount("Foam Amount", Float) = 1
		_FoamCutoff("Foam Cutoff", Float) = 1
		_CausticsDensity("Caustics Density", Float) = 1
		_CausticsStrength("Caustics Strength", Float) = 1
		_CausticsPower("Caustics Power", Float) = 1
		_CausticsMovement("Caustics Movement", Vector) = (1,0,1)
		_WaterfallColor("Waterfall Color", Color) = (1,1,1,1)
		_WaterfallFoamScale("Waterfall Foam Scale", Float) = 10
		_WaterfallFoamSpeed("Waterfall Foam Speed", Float) = 0.01
		_WaterfallFoamDistance("Waterfall Foam Distance", Float) = 0.2
		_WaterfallFoamAmount("Waterfall Foam Amount", Float) = 1
		_WaterfallFoamStrength("Waterfall Foam Strength", Float) = 1
		_WobbleAmount("Waterfall Wobble Amount", Float) = 1
		_WobbleFrequency("Waterfall Wobble Frequency", Float) = 1
		_WobbleDensity("Waterfall Wobble Density", Float) = 1
		_UnderwaterSilhouetteColor("Underwater Silhouette Color", Color) = (1,1,1,1)
		_UnderwaterSkyColor("Underwater Sky Color", Color) = (1,1,1,1)
		_UnderwaterSkyDepth("Underwater Sky Depth", Float) = 1
		_CausticsNoiseTexture("Caustics/Foam (R) Noise (G) Texture", 2D) = "black" {}

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
            ZWrite On

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
			#define REQUIRE_OPAQUE_TEXTURE 1

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

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
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
				float4 screenPos : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

			CBUFFER_START(UnityPerMaterial)
				half _Smoothness;
				half _ShadowWobbleAmount;
				half _ReflectionAmount;
				half _NormalStrength;
				half _NormalSpeed;
				half _NormalScale;
				half _RefractionSpeed;
				half _RefractionScale;
				half _RefractionStrength;
				half _ShallowWaterDepth;
				half _DeepWaterTransition;
				half4 _ShallowWaterColor;
				half4 _DeepWaterColor;
				half4 _FoamColor;
				half _FoamScale;
				half _FoamSpeed;
				half _FoamAmount;
				half _FoamCutoff;
				half _CausticsDensity;
				half _CausticsStrength;
				half _CausticsPower;
				half3 _CausticsMovement;
				sampler2D _CausticsNoiseTexture;
				half4 _WaterfallColor;
				half _WaterfallFoamScale;
				half _WaterfallFoamSpeed;
				half _WaterfallFoamDistance;
				half _WaterfallFoamAmount;
				half _WaterfallFoamStrength;
				half _WobbleAmount;
				half _WobbleFrequency;
				half _WobbleDensity;
				half4 _UnderwaterSilhouetteColor;
				half4 _UnderwaterSkyColor;
				half _UnderwaterSkyDepth;
			CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 newVertex = VertexWobble(v.vertex.xyz, v.normal, v.tangentOS, _WobbleAmount, _WobbleFrequency, _WobbleDensity);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(newVertex);
                o.vertex = vertexInput.positionCS;
				o.worldPos = vertexInput.positionWS;
				o.miscData.x = ComputeFogFactor(vertexInput.positionCS.z);
				half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
				VertexNormalInputs normalInput = GetVertexNormalInputs(v.normal, v.tangentOS);
				o.normal = half4(normalInput.normalWS, viewDirWS.x);
				o.tangent = half4(normalInput.tangentWS, viewDirWS.y);
				o.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);

				o.miscData.y = CalculateSteepness(o.normal);
				o.miscData.z = v.color.r;
				o.uv = v.uv;
				o.screenPos = ComputeScreenPos(o.vertex);

				OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
				OUTPUT_SH(o.normal.xyz, o.vertexSH);
                return o;
            }

			float EyeDepth(float2 uv)
			{
				return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
			}

			float GetSurfaceDepth(float3 worldPos)
			{
				float4 clip = TransformWorldToHClip(worldPos);
				float surfaceDepth = clip.z / clip.w;//GetSurfaceDepth(i.worldPos);
				return LinearEyeDepth(surfaceDepth, _ZBufferParams);
			}

            half4 frag (v2f i, half facing : VFACE) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				facing = step(0.5, facing);

				float3 screenPos = i.screenPos.xyz / i.screenPos.w;
				half3 normalTS = WaterNormal(TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), i.uv, _NormalSpeed, _NormalScale, _NormalStrength);
				half3 normal = TransformTangentToWorld(normalTS, half3x3(i.tangent.xyz, i.bitangent.xyz, i.normal.xyz));
				half3 viewDirWS = SafeNormalize(half3(i.normal.w, i.tangent.w, i.bitangent.w));

				float3x3 tangentMatrix = float3x3(i.tangent.xyz, i.bitangent.xyz, i.normal.xyz);

				float3 refraction = Refraction(i.uv, _RefractionScale, _RefractionSpeed, _RefractionStrength, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap),
					i.worldPos, tangentMatrix, screenPos);

				half4 sceneColor = half4(SampleSceneColor(refraction), 1);
				float surfaceDepth = GetSurfaceDepth(i.worldPos);
				float depth = EyeDepth(refraction);

				// If we refract so hard that we sample an object that is in front of the water we must fall back to the non-refracted value
				float detectedObjectInFront = step(depth, surfaceDepth);
				depth = lerp(depth, EyeDepth(screenPos), detectedObjectInFront);
				sceneColor = lerp(sceneColor, half4(SampleSceneColor(screenPos), 1), detectedObjectInFront);

				float waterDepth = WaterDepth(_ShallowWaterDepth, _DeepWaterTransition, depth, i.worldPos, i.normal.xyz, i.screenPos);
				half4 waterColor = lerp(_ShallowWaterColor, _DeepWaterColor, waterDepth);

				half4 col = lerp(sceneColor, waterColor, waterColor.a);
				// Any transparency will be done through sceneColor to support refractions
				col.a = 1.0;
				half4 underwaterColor = UnderwaterColor(depth, _UnderwaterSkyDepth, _UnderwaterSilhouetteColor, _UnderwaterSkyColor, sceneColor);
				col = lerp(underwaterColor, col, facing);
				half3 caustics = Caustics(_CausticsDensity, _CausticsStrength, _CausticsPower,_CausticsMovement, _CausticsNoiseTexture, depth, viewDirWS);
				caustics = lerp(0, caustics, min(facing, 1 - waterDepth));
				col += half4(caustics.rgb, 0);

				half steepness = i.miscData.y;

				normal = lerp(normal, i.normal.xyz, steepness);
				normal = NormalizeNormalPerPixel(normal);

				half3 emission = half3(0, 0, 0);
				half3 bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, normal);

				float smoothness = lerp(0, _Smoothness, facing);

				float foam = Foam(i.uv, _FoamScale, _FoamSpeed, _FoamAmount, _FoamCutoff, steepness, 
					_CausticsNoiseTexture, depth, i.worldPos, i.normal.xyz, i.screenPos);
				foam = lerp(0, foam, facing);

				float4 foamCol = lerp(col, _FoamColor, _FoamColor.a);
				col = lerp(col, foamCol, foam);

				col += Waterfall(_WaterfallColor, _WaterfallFoamScale, _WaterfallFoamSpeed, steepness, _WaterfallFoamDistance,
					_WaterfallFoamAmount, _WaterfallFoamStrength, _CausticsNoiseTexture, i.uv);
				col = WaterLighting(col.rgb, col.a, smoothness, _ShadowWobbleAmount, i.worldPos,
					normal, viewDirWS, bakedGI, emission, i.miscData.x, _ReflectionAmount);
				col.a = lerp(0, col.a, i.miscData.z);

				col.rgb = LumaBasedReinhardToneMapping(col.rgb);
				col.rgb = AdjustAccordingToLUT(col.rgb);
				return col;
            }
            ENDHLSL
        }
    }
}
