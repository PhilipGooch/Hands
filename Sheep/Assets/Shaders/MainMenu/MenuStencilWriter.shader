Shader "Universal Render Pipeline/Menu Stencil Writer"
{
    Properties
    {
        _StencilMask("Stencil Mask", int) = 1
        _BaseMap("BaseMap", 2D) = "white" {}
    }
        SubShader
    {
        LOD 100
		Tags { "RenderType" = "Opaque" "Queue" = "Transparent+1" }

        Pass
		{

			ZWrite Off

			Stencil {
				Ref[_StencilMask]
				Comp Always
				Pass Replace
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct fout {
				half4 color : COLOR;
				float depth : DEPTH;
			};

			sampler2D _BaseMap;
			float4 _BaseMap_ST;

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fout o;
			// sample the texture
			fixed4 col = tex2D(_BaseMap, i.uv);
			// apply fog
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
			}
			ENDCG
		}
    }
}