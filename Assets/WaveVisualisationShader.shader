Shader "Unlit/WaveVisualisationShader"
{
    Properties
    {
        _CurrentField ("Current Field", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CurrentField;
            float4 _CurrentField_TexelSize;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 field = tex2D(_CurrentField, i.uv).rg;

                float amplitude = field.x;
                float3 color = 25. * amplitude;

                // float velocity = field.y;
                // float2 f = float2(amplitude, velocity * 2.0);
                // float3 color = 0.25 * dot(f, f);

                return float4(color, 1);
            }
            ENDCG
        }
    }
}
