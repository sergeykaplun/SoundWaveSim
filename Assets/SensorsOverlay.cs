using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class SensorsOverlay : MonoBehaviour
{
    public ComputeShader detectorShader;
    public WaveSimulation WaveSimulation;
    private ComputeBuffer[] _sensorBuffer;
    private ComputeBuffer _TriggerTimeBuffer;
    
    public AudioClip explosionSound;
    public float[] explosionSampleData;
    private ComputeBuffer explosionSamplingBuffer;
    private int explosionSampleRate;
    
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        
        var dataSize = SimulationState.SAMPLE_RATE * SimulationState.SAMPLING_DURATION; //10sec * 1kHz
        float[] zeroData = new float[dataSize];
        
        _sensorBuffer = new ComputeBuffer[SimulationState.mikesPositions.Length];
        for (int i = 0; i < _sensorBuffer.Length; i++)
        {
            _sensorBuffer[i] = new ComputeBuffer(dataSize, sizeof(float), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            _sensorBuffer[i].name = "SensorOutput" + i;
            _sensorBuffer[i].SetData(zeroData);
        }
        
        _TriggerTimeBuffer = new ComputeBuffer(SimulationState.mikesPositions.Length, sizeof(int), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        var data = Enumerable.Repeat(-1, SimulationState.mikesPositions.Length).ToArray();
        _TriggerTimeBuffer.SetData(data);
        _TriggerTimeBuffer.name = "_TriggerTimeBuffers";
        
        for (int i = 0; i < SimulationState.mikesPositions.Length; i++)
        {
            detectorShader.SetBuffer(0, "SensorOutput" + i, _sensorBuffer[i]);
        }
        detectorShader.SetBuffer(0, "SensorTriggeringTime", _TriggerTimeBuffer);
        
        material = GetComponent<Renderer>().material;
        
        material.SetVector("EmitterPosition", SimulationState.emitterPosition);
        for (int i = 0; i < SimulationState.mikesPositions.Length; i++)
        {
            material.SetBuffer("SensorOutput" + i, _sensorBuffer[i]);
        }
        
        CurrentFrameIndex = 0;
        mainCamera = Camera.main;
        mainCamera.transform.LookAt(Vector3.zero);
        
        
        if (explosionSound == null)
        {
            Debug.LogError("No explosion sound assigned.");
            return;
        }

        int channels = explosionSound.channels;
        int totalSamples = explosionSound.samples * channels;
        explosionSampleRate = explosionSound.frequency;

        float[] sampleData = new float[totalSamples];
        explosionSound.GetData(sampleData, 0);

        if (channels == 1)
        {
            explosionSampleData = sampleData;
        }
        else
        {
            int monoSampleCount = explosionSound.samples;
            explosionSampleData = new float[monoSampleCount];

            for (int i = 0; i < monoSampleCount; i++)
            {
                explosionSampleData[i] = sampleData[i * channels + 0];
            }
        }
    }

    private int CurrentFrameIndex;
    private Material material;
    private Camera mainCamera;
    private int? draggingItem;
    // private Vector2 selectedCircleUV;
    private bool mousePressedThisFrame;
    private bool mousePressedLastFrame;

    void Update()
    {
        // Detect if mouse was pressed this frame and not in the previous frame
        mousePressedThisFrame = Input.GetMouseButtonDown(0) && !mousePressedLastFrame;
        mousePressedLastFrame = Input.GetMouseButton(0);
        
        // Detect mouse button press
        if (mousePressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the raycast hits the plane where circles are drawn
            if (Physics.Raycast(ray, out hit))
            {
                Vector2 uv;
                if (TryGetUV(hit, out uv))
                {
                    if (Vector4.Distance(SimulationState.emitterPosition, new(uv.x, uv.y, 0, 0)) < 0.05)
                    {
                        draggingItem = -1; 
                    }
                    var microphones = SimulationState.mikesPositions;
                    for (int i = 0; i < microphones.Length; i++)
                    {
                        if (Vector4.Distance(microphones[i], new(uv.x, uv.y, 0, 0)) < 0.05)
                        {
                            draggingItem = i; 
                        }
                    }
                }
            }
        }

        // Dragging the selected circle
        if (draggingItem != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector2 uv;
                if (TryGetUV(hit, out uv))
                {
                    if((int)draggingItem == -1)
                    {
                        SimulationState.emitterPosition = new Vector4(uv.x, uv.y, 0, 0);
                    }
                    else
                    {
                        SimulationState.mikesPositions[(int)draggingItem] = new Vector4(uv.x, uv.y, 0, 0);
                    }
                }
            }
        }
        
        // Release the circle
        if (Input.GetMouseButtonUp(0))
        {
            draggingItem = null;
        }
        
        Vector4[] positions = SimulationState.GetMikesPositionsInPixels();
        detectorShader.SetVectorArray("MicrophonePositions", positions);
        detectorShader.SetInt("CurFrameIndex", CurrentFrameIndex);
        detectorShader.SetTexture(0, "SoundField", WaveSimulation.GetSimulationField());
        detectorShader.Dispatch(0, 1, 1, 1);
        
        material.SetVector("EmitterPosition", SimulationState.emitterPosition);
        material.SetVectorArray("MicrofonesPositions", SimulationState.mikesPositions);
        material.SetInt("CurFrameIndex", CurrentFrameIndex);
        material.SetInt("DraggingItem", draggingItem ?? 999);
        
        CurrentFrameIndex++;
    }
    
    // Helper method to get the UV coordinates from a raycast hit
    bool TryGetUV(RaycastHit hit, out Vector2 uv)
    {
        uv = Vector2.zero;
        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (!meshCollider || !meshCollider.sharedMesh)
        {
            return false;
        }

        Mesh mesh = meshCollider.sharedMesh;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        // Get the barycentric coordinates of the hit point
        Vector3 barycentricCoord = hit.barycentricCoordinate;
        int hitTriangleIndex = hit.triangleIndex * 3;

        Vector2 uv1 = uvs[triangles[hitTriangleIndex]];
        Vector2 uv2 = uvs[triangles[hitTriangleIndex + 1]];
        Vector2 uv3 = uvs[triangles[hitTriangleIndex + 2]];

        // Calculate the UV coordinate using barycentric interpolation
        uv = uv1 * barycentricCoord.x + uv2 * barycentricCoord.y + uv3 * barycentricCoord.z;
        return true;
    }
    
    private int[] GetTriggeringFrames()
    {
        int[] times = new int[SimulationState.mikesPositions.Length];
        _TriggerTimeBuffer.GetData(times);
        return times;
    }
    
    public void SaveData()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string folderName = "Data_" + timestamp;
            string folderPath = "Assets/" + folderName;

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", folderName);
            }
            else
            {
                Assert.IsTrue(false, "Folder already exists");
            }
            
            int channels = 1;
            var filenames = Enumerable.Range(1, SimulationState.mikesPositions.Length).Select(i => i.ToString() + ".wav").ToArray();
            
            for (int i = 0; i < filenames.Length; i++)
            {
                string filename = filenames[i];
                float[] data = new float[SimulationState.SAMPLE_RATE * SimulationState.SAMPLING_DURATION];
                _sensorBuffer[i].GetData(data);

                using (FileStream fileStream = new FileStream(folderPath + "/" + filename, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        int byteRate = SimulationState.SAMPLE_RATE * channels * 2; // 16-bit audio
                        // int byteRate = explosionSampleRate * sizeof(float);
                        short bitsPerSample = 16;
                        
                        // Create WAV header
                        writer.Write(Encoding.ASCII.GetBytes("RIFF")); // RIFF header
                        writer.Write(36 + SimulationState.SAMPLE_RATE * SimulationState.SAMPLING_DURATION * 2); // File size
                        writer.Write(Encoding.ASCII.GetBytes("WAVE")); // WAVE header
                        
                        // Format chunk
                        writer.Write(Encoding.ASCII.GetBytes("fmt "));
                        writer.Write(16); // Size of format chunk
                        writer.Write((short)1); // Audio format (1 = PCM)
                        writer.Write((short)channels); // Number of channels
                        writer.Write(SimulationState.SAMPLE_RATE); // Sample rate
                        writer.Write(byteRate); // Byte rate
                        writer.Write((short)(channels * bitsPerSample / 8)); // Block align
                        writer.Write(bitsPerSample); // Bits per sample
                        
                        // Data chunk
                        writer.Write(Encoding.ASCII.GetBytes("data"));
                        writer.Write(SimulationState.SAMPLE_RATE * SimulationState.SAMPLING_DURATION * 2); // Data chunk size
                        
                        // Write audio data
                        foreach (float sample in data)
                        {
                            short intSample = (short)(sample * short.MaxValue);
                            writer.Write(intSample);
                        }
                        writer.Flush();
                    }
                }
            }

            int[] triggeringFrames = GetTriggeringFrames();
            int framesLag = triggeringFrames[1] - triggeringFrames[0];
            float distanceBetweenMikes = Vector4.Distance(SimulationState.mikesPositions[0], SimulationState.mikesPositions[1]) * SimulationState.SOUND_FIELD_SIZE;
            Debug.Log("Distance between mikes: " + distanceBetweenMikes);
            Debug.Log("Frames lag: " + framesLag);
           {
                for (int i = 0; i < triggeringFrames.Length; i++)
                {
                    triggeringFrames[i] = (int)(triggeringFrames[i] * ((float)explosionSampleRate)/SimulationState.SAMPLE_RATE);
                }
                
                int minTriggeringFrame = triggeringFrames.Min();
                triggeringFrames = triggeringFrames.Select(f => f - minTriggeringFrame).ToArray();
                Assert.IsTrue(triggeringFrames.Any(f => f >= 0));
                int maxTriggeringFrame = triggeringFrames.Max();
                
                int sampleCount = explosionSampleData.Length + maxTriggeringFrame;
                int explsionByteRate = explosionSampleRate * 2;
                
                var mock_filenames = Enumerable.Range(1, SimulationState.mikesPositions.Length).Select(i => i.ToString() + "_mock.wav").ToArray();
                for (int i = 0; i < mock_filenames.Length; i++)
                {
                    string filename = mock_filenames[i];
                    using (FileStream fileStream = new FileStream(folderPath + "/" + filename, FileMode.Create))
                    {
                        using (BinaryWriter writer = new BinaryWriter(fileStream))
                        {
                            // Create WAV header
                            writer.Write(Encoding.ASCII.GetBytes("RIFF")); // RIFF header
                            writer.Write(36 + sampleCount * 2); // File size
                            writer.Write(Encoding.ASCII.GetBytes("WAVE")); // WAVE header

                            // Format chunk
                            writer.Write(Encoding.ASCII.GetBytes("fmt "));
                            writer.Write(16); // Size of format chunk
                            writer.Write((short)1); // Audio format (1 = PCM)
                            writer.Write((short)1); // Number of channels
                            writer.Write(explosionSampleRate); // Sample rate
                            writer.Write(explsionByteRate); // Byte rate
                            writer.Write((short)(1 * 16 / 8)); // Block align
                            writer.Write((short)16); // Bits per sample

                            // Data chunk
                            writer.Write(Encoding.ASCII.GetBytes("data"));
                            writer.Write(sampleCount * 2); // Data chunk size

                            // Write audio data
                            for (int j = 0; j < triggeringFrames[i]; j++)
                            {
                                short intSample = 0;
                                writer.Write(intSample);
                            }
                            for (int j = 0; j < explosionSampleData.Length; j++)
                            {
                                short intSample = (short)(explosionSampleData[j] * short.MaxValue);
                                writer.Write(intSample);
                            }

                            writer.Flush();
                        }
                    }
                }
            }
            var jsonDescriptorPattern = @"{{
                    ""EmitterPosition"": [{0}, {1}],
                    ""MikesPositions"": [[{2}, {3}], [{4}, {5}], [{6}, {7}], [{8}, {9}], [{10}, {11}], [{12}, {13}]],
                    ""SimulationResolution"": {14},
                    ""SimulationSpace"": {15},
                    ""sampleRate"": {16}
                }}";
            
            string jsonDescriptor = string.Format(jsonDescriptorPattern, 
                SimulationState.emitterPosition.x, SimulationState.emitterPosition.y,
                SimulationState.mikesPositions[0].x, SimulationState.mikesPositions[0].y,
                SimulationState.mikesPositions[1].x, SimulationState.mikesPositions[1].y,
                SimulationState.mikesPositions[2].x, SimulationState.mikesPositions[2].y,
                SimulationState.mikesPositions[3].x, SimulationState.mikesPositions[3].y,
                SimulationState.mikesPositions[4].x, SimulationState.mikesPositions[4].y,
                SimulationState.mikesPositions[5].x, SimulationState.mikesPositions[5].y,
                SimulationState.GRID_RES, SimulationState.SOUND_FIELD_SIZE,
                SimulationState.SAMPLE_RATE);
            File.WriteAllText(folderPath + "/descriptor.json", jsonDescriptor);
            Debug.Log($"Files saved successfully to folder: {folderName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save WAV file: {e.Message}");
            throw;
        }
    }
}
