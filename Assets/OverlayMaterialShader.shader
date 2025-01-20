Shader "Unlit/OverlayMaterialShader"
{
    Properties
    {
//        _Source ("Source", Vector) = (0.0, 0.0, 0.0, 0.0)
//        _Microphone1 ("Microphone_0", Vector) = (0.0, 0.0, 0.0, 0.0)
//        _Microphone0 ("Microphone_1", Vector) = (0.0, 0.0, 0.0, 0.0)
//        _Microphone2 ("Microphone_2", Vector) = (0.0, 0.0, 0.0, 0.0)
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
            // StructuredBuffer<float> SensorOutput6;
            // StructuredBuffer<float> SensorOutput7;
            // StructuredBuffer<float> SensorOutput8;
            
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

            /*float SoundPressure(StructuredBuffer<float> buffer, float2 uv)
            {
                uint FramesSpan = 44100 / 50.0;
                float offset = 0;
                if (CurFrameIndex > FramesSpan)
                {
                    offset = CurFrameIndex - FramesSpan;
                }
                // int index = lerp(offset, FramesSpan + offset, uv.x);
            
                float weights[9] = {1.0, 2.0, 1.0, 2.0, 4.0, 2.0, 1.0, 2.0, 1.0};
                float totalWeight = 16.0;
                float interpolatedValue = 0.0;
            
                for (int i = -1; i <= 1; i++)
                {
                     for (int j = -1; j <= 1; j++)
                    {
                        float2 sampleUV = uv + float2(i, j) * 0.01;
                        int sampleIndex = lerp(offset, FramesSpan + offset, sampleUV.x);
                        float sampleValue = lerp(buffer[sampleIndex], buffer[sampleIndex + 1], frac(sampleUV.x * FramesSpan));
                        interpolatedValue += sampleValue * weights[(i + 1) * 3 + (j + 1)];
                    }
                }
            
                interpolatedValue /= totalWeight;
                return smoothstep(0.0025, 0.002, distance(uv.y, interpolatedValue));
            }*/

            float SoundPressure(StructuredBuffer<float> buffer, float2 uv)
            {
                float avg = buffer[(1.-uv.x) * 40000];
                // for(int i = 0; i < 10; i++)
                // {
                //     int sampleIndex = (uv.x * 20000) - 5 + i;
                //     if (sampleIndex >= 0 && sampleIndex < 20000)
                //     {
                //         float sampleValue = buffer[sampleIndex];
                //         avg += sampleValue;
                //     }
                // }
                // avg /= 10;
                return step(abs(uv.y - 0.5), avg);
                // uint FramesSpan = 44100 / 50.0;
                // float offset = 0;
                // if (CurFrameIndex > FramesSpan)
                // {
                //     offset = CurFrameIndex - FramesSpan;
                // }
                // // int index = lerp(offset, FramesSpan + offset, uv.x);
                //
                // float weights[9] = {1.0, 2.0, 1.0, 2.0, 4.0, 2.0, 1.0, 2.0, 1.0};
                // float totalWeight = 16.0;
                // float interpolatedValue = 0.0;
                //
                // for (int i = -1; i <= 1; i++)
                // {
                //      for (int j = -1; j <= 1; j++)
                //     {
                //         float2 sampleUV = uv + float2(i, j) * 0.01;
                //         int sampleIndex = lerp(offset, FramesSpan + offset, sampleUV.x);
                //         float sampleValue = lerp(buffer[sampleIndex], buffer[sampleIndex + 1], frac(sampleUV.x * FramesSpan));
                //         interpolatedValue += sampleValue * weights[(i + 1) * 3 + (j + 1)];
                //     }
                // }
                //
                // interpolatedValue /= totalWeight;
                // return smoothstep(0.0025, 0.002, distance(uv.y, interpolatedValue));
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
                    // SensorOutput6,
                    // SensorOutput7,
                    // SensorOutput8
                };
                float4 colors[6] = {
                    /*float4(1.0, 0.0, 1.0, 1.0),
                    float4(0.0, 1.0, 1.0, 1.0),
                    float4(1.0, 1.0, 0.0, 1.0),
                    float4(0.0, 0.0, 1.0, 1.0),
                    float4(0.0, 1.0, 0.0, 1.0),
                    float4(0.0, 1.0, 1.0, 1.0),
                    float4(1.0, 1.0, 0.0, 1.0),
                    float4(0.0, 0.0, 1.0, 1.0),
                    float4(0.0, 1.0, 0.0, 1.0),*/
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
                    float4(0.0, 1.0, 1.0, 1.0),
                     // 7. Blue
                    // float4(0.0, 0.0, 1.0, 1.0),
                    //  // 8. Indigo
                    // float4(0.3, 0.0, 0.5, 1.0),
                    //  // 9. Violet
                    // float4(0.5, 0.2, 0.9, 1.0),
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
                    //if(distance(i.uv.y, 0.1 + 0.2 * index) < (0.1 + 0.2 * index))
                    {
                        float2 muv = i.uv;
                        muv.y = (muv.y - 0.2 * index - 0.1);
                        clr += colors[index] * SoundPressure(buffers[index], muv * float2(1.0, 5.) + float2(0.0, 0.5));
                    }
                }
                return clr;
            }
            ENDCG
        }
    }
}
