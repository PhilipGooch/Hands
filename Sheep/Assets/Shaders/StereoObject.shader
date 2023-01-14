
Shader "Stereo/StereoObject" {
	Properties{
		_Tint("Tint Color", Color) = (.5, .5, .5, .5)
		_TexLeft("LEFT", 2D) = "grey" {}
		_TexRight("RIGHT", 2D) = "grey" {}
	}

		SubShader{
			Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			Cull Off ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			
			Pass {

				HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"     
				#include "General/ShaderUtils.hlsl"

				sampler2D _TexLeft;
				sampler2D _TexRight;
				float4 _TexLeft_ST;

				half4 _Tint;

				struct appdata_t {
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				v2f vert(appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					o.vertex = TransformObjectToHClip(v.vertex);
					o.texcoord = v.texcoord;
					return o;
				}

				half4 frag(v2f i) : SV_Target
				{
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

					half4 tex;
					half4 c;

					if (unity_StereoEyeIndex == 0) {
						// Left Eye
						c = tex2D(_TexLeft, i.texcoord);
					}
					else {
						// Right Eye
						c = tex2D(_TexRight, i.texcoord);
					}

					c = c * half4(_Tint.rgb, c.a);

					c.rgb = LumaBasedReinhardToneMapping(c.rgb);
					c.rgb = AdjustAccordingToLUT(c.rgb);
					return half4(c);
				}
				ENDHLSL
			}
	}


		Fallback Off

}