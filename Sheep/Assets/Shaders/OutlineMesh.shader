Shader "Universal Render Pipeline/Outline Mesh"
{
	Properties
	{
		[MainTexture] _BaseMap("Main Texture", 2D) = "white" {}
		_Color("Color", Color) = (0,0,1,1)
		_Width("Width", Float) = 0.5
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"}
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest LEqual
			Cull Front

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
				float3 normal : NORMAL;
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _BaseMap;
			float4 _BaseMap_ST;
			float _Fresnel;
			float _FresnelPower;
			float4 _Color;
			float _Width;

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float4 clipPos = UnityObjectToClipPos(v.vertex);
				float3 clipNormal = mul((float3x3) UNITY_MATRIX_MVP, v.normal);
				float oculusResolutionWidth = 1920;
				float finalWidth = _Width * (_ScreenParams.x / oculusResolutionWidth);
				float2 offset = normalize(clipNormal.xy) / _ScreenParams.xy * finalWidth * clipPos.w * 2;
				clipPos.xy += offset;
				o.vertex = clipPos;
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.normal = UnityObjectToWorldNormal(v.normal);
				o.viewDir = normalize(_WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float alpha = tex2D(_BaseMap, i.uv).a;
				return _Color * alpha;
			}
			ENDHLSL
		}
	}
}
