
Shader "Universal Render Pipeline/Menu Stencil Depth Nuke"
{
    Properties
    {
        _StencilMask("Stencil Mask", int) = 1
    }
        SubShader
    {
        LOD 100
		Tags { "RenderType" = "Opaque" "Queue" = "Transparent+2" }

		ZWrite On
		ZTest Always
		ColorMask 0

		Stencil {
			Ref[_StencilMask]
			Comp Equal
			Pass Keep
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct fout {
				half4 color : COLOR;
				float depth : DEPTH;
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			float frag(v2f i) : SV_DEPTH
			{
				return 0;
			}
			ENDCG
		}
    }
}
