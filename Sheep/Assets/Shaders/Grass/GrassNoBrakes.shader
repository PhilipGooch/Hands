Shader "Unlit/GrassNoBrakes"
{
    Properties
    {
		[MainTexture] _BaseMap("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}
		[MainColor]   _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		[HideInInspector] _Surface("__surface", Float) = 0.0

		_NoiseTex("Noise Texture", 2D) = "white" {}
		_MainNoiseScale("Main Noise Scale", Float) = 1
		_MainNoiseStrength("Main Noise Strength", Float) = 1
		_SecondaryNoiseScale("Secondary Noise Scale", Float) = 2
		_SecondaryNoiseStrength("Secondary Noise Strength", Float) = 2
		_NoiseBlend("Noise Blend", Float) = 0.5
		_WindStrength("Wind Strength", Range(0,1)) = 1
		_SecondaryWindStrength("Secondary Wind Strength", Range(0,1)) = 0
		_WindSpeed("Wind Speed", Float) = 1
		_WindScale("Wind Scale", Float) = 0.1
		_TintTop("Tint Top", Float) = 0.2
    }
    SubShader
    {
		Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "SimpleLit" "IgnoreProjector" = "True" "ShaderModel" = "4.5" }
        LOD 100
		Cull Off

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On

			HLSLPROGRAM
			#pragma target 4.5

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _EMISSION
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local_fragment _ _SPECGLOSSMAP _SPECULAR_COLOR
			#pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

			// -------------------------------------
			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _LIGHT_LAYERS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#pragma multi_compile _ _CLUSTERED_RENDERING

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON
			#pragma multi_compile_fog
			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#pragma vertex LitPassVertexGrass
			#pragma fragment LitPassFragmentGrass
			#define BUMP_SCALE_NOT_SUPPORTED 1
			#define _ALPHATEST_ON 1

			#include "GrassLitOverrides.hlsl"
			#include "../General/ShaderUtils.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			sampler2D _NoiseTex;

			struct Attributes
			{
				half4 positionOS    : POSITION;
				half3 normalOS      : NORMAL;
				half4 tangentOS     : TANGENT;
				half2 texcoord      : TEXCOORD0;
				half2 staticLightmapUV    : TEXCOORD1;
				half2 dynamicLightmapUV   : TEXCOORD2;
				half4 color	     : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				half2 uv                       : TEXCOORD0;
				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);

				half3 posWS                    : TEXCOORD2;    // xyz: posWS

			#ifdef _NORMALMAP
				half4 normal                   : TEXCOORD3;    // xyz: normal, w: viewDir.x
				half4 tangent                  : TEXCOORD4;    // xyz: tangent, w: viewDir.y
				half4 bitangent                : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
			#else
				half3 normal                   : TEXCOORD3;
				half3 viewDir                  : TEXCOORD4;
			#endif

				half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				half4 shadowCoord              : TEXCOORD7;
			#endif

				half4 positionCS               : SV_POSITION;
				half4 color					: COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};


			half SampleTriplanarNoise(half scale, half2 offset, half3 worldPos, half3 normal)
			{
				half2 uv = CheapAsChipsTriplanar(worldPos * scale + offset.x, normal);
				half3 y = tex2D(_NoiseTex, uv).rgb;
				return y.x;
			}

			// Used in Standard (Simple Lighting) shader
			Varyings LitPassVertexGrass(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
				half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
				half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
				output.posWS.xyz = vertexInput.positionWS;
				output.positionCS = vertexInput.positionCS;

#ifdef _NORMALMAP
				output.normal = half4(normalInput.normalWS, viewDirWS.x);
				output.tangent = half4(normalInput.tangentWS, viewDirWS.y);
				output.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
#else
				output.normal = NormalizeNormalPerVertex(normalInput.normalWS);
				output.viewDir = viewDirWS;
#endif

				OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
				OUTPUT_SH(output.normal.xyz, output.vertexSH);

				output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				output.shadowCoord = GetShadowCoord(vertexInput);
#endif
				output.color = input.color;

				return output;
			}

			//Copied from SimpleLitForwardPass.hlsl
			void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
			{
				inputData = (InputData)0;

				inputData.positionWS = input.posWS;

#ifdef _NORMALMAP
				half3 viewDirWS = half3(input.normal.w, input.tangentWS.w, input.bitangentWS.w);
				inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normal.xyz);
				inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
#else
				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
				inputData.normalWS = input.normal;
#endif

				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				viewDirWS = SafeNormalize(viewDirWS);

				inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
				inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
				inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
				inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
				inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
				inputData.vertexLighting = half3(0, 0, 0);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
				inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#else
				inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
				inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

#if defined(DEBUG_DISPLAY)
#if defined(DYNAMICLIGHTMAP_ON)
				inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
#endif
#if defined(LIGHTMAP_ON)
				inputData.staticLightmapUV = input.staticLightmapUV;
#else
				inputData.vertexSH = input.vertexSH;
#endif
#endif
			}

			half4 LitPassFragmentGrass(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				half2 uv = input.uv;
				half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
				half3 diffuse = diffuseAlpha.rgb * _BaseColor.rgb;

				half height = input.color.r;

				half windAmount = (tex2D(_NoiseTex, abs(input.posWS.xz * _WindScale + _Time.x * _WindSpeed)).r * 2) - 1;
				half2 windOffset = windAmount * (height);

				half mainNoise = SampleTriplanarNoise(_MainNoiseScale, windOffset * _WindStrength, input.posWS, input.normal);
				half secondaryNoise = SampleTriplanarNoise(_SecondaryNoiseScale, windOffset * _SecondaryWindStrength, input.posWS, input.normal);
				mainNoise = saturate(mainNoise * _MainNoiseStrength);
				secondaryNoise = saturate(secondaryNoise * _SecondaryNoiseStrength);

				half finalNoise = lerp(mainNoise, secondaryNoise, _NoiseBlend);


				half alphaMod = step(height, finalNoise);

				half alpha = diffuseAlpha.a * _BaseColor.a * alphaMod;
				AlphaDiscard(alpha, 0.1);

				#ifdef _ALPHAPREMULTIPLY_ON
					diffuse *= alpha;
				#endif

				half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
				half3 emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
				half4 specular = SampleSpecularSmoothness(uv, alpha, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
				half smoothness = specular.a;

				InputData inputData;
				InitializeInputData(input, normalTS, inputData);

				half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specular, smoothness, emission, alpha, normalTS);
				half4 tintedColor = saturate(color * (1.0 + _TintTop));
				color = lerp(color, tintedColor, height);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				color.a = OutputAlpha(color.a, _Surface);

				color.rgb = LumaBasedReinhardToneMapping(color.rgb);
				color.rgb = AdjustAccordingToLUT(color.rgb);
				return color;
			}
			ENDHLSL
        }

		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull Off

			HLSLPROGRAM
			#pragma target 4.5

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

			#pragma vertex ShadowGrassVertex
			#pragma fragment ShadowPassFragment

			#include "GrassLitOverrides.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

			struct shadowInput
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float2 texcoord     : TEXCOORD0;
				float4 color		: COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings ShadowGrassVertex(shadowInput input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);

				float4 positionOS = input.positionOS;
				float positionModifier = 1 - input.color.r;
				// Only draw shadows with the bottom-most layer. Ignore all other layers
				positionModifier = step(1, positionModifier);
				positionOS *= positionModifier;

				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
				Attributes attributes;
				attributes.positionOS = positionOS;
				attributes.normalOS = input.normalOS;
				attributes.texcoord = input.texcoord;
				output.positionCS = GetShadowPositionHClip(attributes);

				return output;
			}
			ENDHLSL
		}

		// Used for depth prepass
		// If shadows cascade are enabled we need to perform a depth prepass. 
		// We also need to use a depth prepass in some cases camera require depth texture
		// (e.g, MSAA is enabled and we can't resolve with Texture2DMS
		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull Off

			HLSLPROGRAM
			#pragma target 4.5

			#pragma vertex DepthOnlyGrassVertex
			#pragma fragment DepthOnlyFragment

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "GrassLitOverrides.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

			struct DepthInput
			{
				float4 position     : POSITION;
				float2 texcoord     : TEXCOORD0;
				float4 color		: COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings DepthOnlyGrassVertex(DepthInput input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float4 positionOS = input.position;
				float positionModifier = 1 - input.color.r;
				// Only write depth with the bottom-most layer. Ignore all other layers
				positionModifier = step(1, positionModifier);
				positionOS *= positionModifier;

				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
				output.positionCS = TransformObjectToHClip(positionOS.xyz);
				return output;
			}

			ENDHLSL
		}

		// This pass it not used during regular rendering, only for lightmap baking.
		Pass
		{
			Name "Meta"
			Tags{ "LightMode" = "Meta" }

			Cull Off

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma vertex UniversalVertexMeta
			#pragma fragment UniversalFragmentMetaSimple

			#pragma shader_feature_local_fragment _EMISSION
			#pragma shader_feature_local_fragment _SPECGLOSSMAP

			#include "GrassLitOverrides.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitMetaPass.hlsl"

			ENDHLSL
		}
    }
}
