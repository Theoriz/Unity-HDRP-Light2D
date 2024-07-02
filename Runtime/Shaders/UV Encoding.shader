Shader "Hidden/Custom/UV Encoding"
{
    Properties
    {
        _MainTex("Texture", 2D) = "black" {}
    }

    SubShader
    {
		Tags{"RenderPipeline" = "HDRenderPipeline"}

        Pass
        {
            Name "GrayScale"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;

			float4 frag(v2f i) : SV_Target
			{
				float alpha = tex2D(_MainTex, i.uv).a;
				float4 uvEncoding = float4(i.uv, 1, alpha) * (1. - step(alpha, 0));
				return uvEncoding;
			}
            ENDHLSL
        }
    }

    Fallback Off
}
