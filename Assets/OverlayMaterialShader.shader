Shader "Unlit/OverlayMaterialShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols

            float4 EmitterPosition;
            float4 MicrofonesPositions[6];
            int CurFrameIndex;
            int DraggingItem = 999;

            StructuredBuffer<float> SensorOutput0;
            StructuredBuffer<float> SensorOutput1;
            StructuredBuffer<float> SensorOutput2;
            StructuredBuffer<float> SensorOutput3;
            StructuredBuffer<float> SensorOutput4;
            StructuredBuffer<float> SensorOutput5;
            
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

            float SoundPressure(StructuredBuffer<float> buffer, float2 uv)
            {
                float avg = buffer[(1.-uv.x) * 40000];
                return step(abs(uv.y - 0.5), avg);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 clr = 0;
                clr = lerp(clr, float4(1.0, 0.0, 0.0, 1.0), smoothstep(0.0025, 0.001, distance(0.005, distance(i.uv, EmitterPosition.xy))));

                StructuredBuffer<float> buffers[6] = {
                    SensorOutput0,
                    SensorOutput1,
                    SensorOutput2,
                    SensorOutput3,
                    SensorOutput4,
                    SensorOutput5,
                };
                float4 colors[6] = {
                     // 1. Red
                    float4(1.0, 0.0, 0.0, 1.0),
                     // 2. Orange
                    float4(1.0, 0.5, 0.0, 1.0),
                     // 3. Yellow
                    float4(1.0, 1.0, 0.0, 1.0),
                     // 4. Light Green
                    float4(0.5, 1.0, 0.0, 1.0),
                     // 5. Green
                    float4(0.0, 1.0, 0.0, 1.0),
                     // 6. Cyan
                    float4(0.0, 1.0, 1.0, 1.0)
                };
                [unroll]
                for (int index=0; index<6; index++)
                {
                    float additionalSize = 0.0;
                    if(DraggingItem == index)
                    {
                        additionalSize = 0.01;
                    }
                    clr = lerp(clr, colors[index], smoothstep(0.006 + additionalSize, 0.004 + additionalSize, distance(0.0075, distance(i.uv, MicrofonesPositions[index].xy))));
                    {
                        float2 muv = i.uv;
                        muv.y *= 6.;
                        muv.y -= 1/3. * index;
                        clr += colors[index] * SoundPressure(buffers[index], muv);
                    }
                }
                return clr;
            }
            ENDCG
        }
    }
}
