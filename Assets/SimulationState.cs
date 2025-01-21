using UnityEngine;

public class SimulationState
{
    public static int SAMPLE_RATE = 1000;
    public static int SAMPLING_DURATION = 60;
    
    public static int GRID_RES = 16000;
    public static int SOUND_FIELD_SIZE = 8000;

    public static float DX = ((float)GRID_RES)/SOUND_FIELD_SIZE;
    public static float DT = DX / SAMPLE_RATE;
    
    public static Vector4 emitterPosition = new(0.5f, 0.5f, 0.0f, 0.0f);
    public static Vector4[] mikesPositions =
    {
        new(0.5f, 0.75f, 0.0f, 0.0f),
        new(0.5f, 0.80f, 0.0f, 0.0f),
        new(0.5f, 0.85f, 0.0f, 0.0f),
        new(0.5f, 0.9f, 0.0f, 0.0f),
        new(0.5f, 0.95f, 0.0f, 0.0f),
        new(0.5f, 1.0f, 0.0f, 0.0f),
    };

    public static Vector4[] GetMikesPositionsInPixels()
    {
        Vector4[] positions = new Vector4[mikesPositions.Length];
        for (int i = 0; i < mikesPositions.Length; i++)
        {
            positions[i] = new Vector4(mikesPositions[i].x * GRID_RES, mikesPositions[i].y * GRID_RES, 0, 0);
        }

        return positions;
    }
    
    public static Vector4 GetEmitterPosition()
    {
        return new Vector4(emitterPosition.x * SOUND_FIELD_SIZE, emitterPosition.y * SOUND_FIELD_SIZE, 0, 0);
    }
}
