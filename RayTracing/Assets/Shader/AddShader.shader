Shader "Hidden/AddShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct VertexIn
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOut
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			VertexOut vert (VertexIn vertexIn)
            {
				VertexOut vertexOut;
				vertexOut.vertex = UnityObjectToClipPos(vertexIn.vertex);
				vertexOut.uv = vertexIn.uv;
                return vertexOut;
            }

            sampler2D _MainTex;
			float _sample;

            float4 frag (VertexOut vertexOut) : SV_Target
            {
				return float4(tex2D(_MainTex, vertexOut.uv).rgb, 1.0f / (_sample + 1.0f));
            }
            ENDCG
        }
    }
}
