Shader "FX/CloudRealistic (Alpha Blended)" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_DarkColor("Dark Color", Color) = (1,1,1,1)
	_LightColor("Light Color", Color) = (1,1,1,1)
	_MaxSunDarken("Max Sun Darken", Range(0,1)) = 0.1
	_RimLightDistance("Rim Light Distance", Range(0,1)) = 0.7
	_RimLightStrength("Rim Light Strength", Range(0,10)) = 1.0
	_DotProductForRimLight("Min Dot Product For Rim Light", Range(0,1)) = 0.5
	_DotProductForSunLight("Min Dot Product For Sun Light", Range(0,1)) = 0.95
	_SunStrength("Sun Strength", Range(0,1)) = 0.95
	_SunOffset("Sun Offset", Vector) = (0,0,0,0)
//	_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	Blend SrcAlpha OneMinusSrcAlpha
	//AlphaTest Greater .01
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {
		
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_particles
			//#pragma multi_compile_fog
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"     
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"     
			#include "../General/ShaderUtils.hlsl"

			sampler2D _MainTex;
			half4 _TintColor;
			
			struct appdata_t {
				half4 vertex : POSITION;
				half4 color : COLOR;
				half3 normal : NORMAL;
				half2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				half4 vertex : SV_POSITION;
				half4 color : COLOR;
				half2 texcoord : TEXCOORD0;
				half3 worldPos : TEXCOORD1;
				half3 normal : NORMAL;
				//UNITY_FOG_COORDS(1)
//				#ifdef SOFTPARTICLES_ON
//				float4 projPos : TEXCOORD2;
//				#endif
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			half4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.worldPos = v.vertex;
//				#ifdef SOFTPARTICLES_ON
//				o.projPos = ComputeScreenPos (o.vertex);
//				COMPUTE_EYEDEPTH(o.projPos.z);
//				#endif
				o.color = v.color;
				o.normal = TransformObjectToWorldNormal(v.normal);
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			//sampler2D_float _CameraDepthTexture;
			half _InvFade;
			half _MaxSunDarken;
			half _RimLightDistance;
			half _RimLightStrength;
			half4 _DarkColor;
			half4 _LightColor;
			half _DotProductForRimLight;
			half _DotProductForSunLight;
			half _SunStrength;
			half4 _SunOffset;

			half4 frag(v2f i) : SV_Target
			{
				//				#ifdef SOFTPARTICLES_ON
				//				float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
				//				float partZ = i.projPos.z;
				//				float fade = saturate (_InvFade * (sceneZ-partZ));
				//				i.color.a *= fade;
				//				#endif
				half4 texColor = tex2D(_MainTex, i.texcoord);
				half4 col = i.color * _TintColor * texColor;
				half3 mainLightDir = -GetMainLight().direction;
				half dotProduct = dot(normalize(i.normal), normalize(mainLightDir));
				half darknessAmount = smoothstep(1.0 - _MaxSunDarken, 1.0, 1.0 - abs(dotProduct));
				half2 closestUV = min(smoothstep(0.0, _RimLightDistance, i.texcoord), smoothstep(0.0, _RimLightDistance, 1.0 - i.texcoord));
				half lightness = 1.0 - min(closestUV.x, closestUV.y);
				//lightness *= smoothstep(_DotProductForRimLight, 1.0 ,saturate(dotProduct));
				//col.rgb += _LightColor * saturate(lightness * _RimLightStrength);

				half3 toCamera = normalize(_WorldSpaceCameraPos - i.worldPos);
				half cameraDot = saturate(dot(toCamera, normalize(mainLightDir + _SunOffset.xyz)));

				lightness *= smoothstep(_DotProductForRimLight, 1.0 , cameraDot);
				col.rgb += _LightColor * saturate(lightness * _RimLightStrength);

				cameraDot = smoothstep(_DotProductForSunLight, 1.0, cameraDot);
				col.rgb += _LightColor * cameraDot * _SunStrength;


				col.rgb = lerp(col.rgb, _DarkColor, darknessAmount);


				col.a *= 4;
				//UNITY_APPLY_FOG(i.fogCoord, col);

				col.rgb = LumaBasedReinhardToneMapping(col.rgb);
				col.rgb = AdjustAccordingToLUT(col.rgb);
				return col;
			}
			ENDHLSL 
		}
	}	
}
}
