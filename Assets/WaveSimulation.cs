using UnityEngine;

public class WaveSimulation : MonoBehaviour
{
    public int fps_frameBufferSize = 120;
    private float[] fps_frameTimes;
    
    public ComputeShader computeShader;
    public TMPro.TMP_Text fpsText;
    
    private RenderTexture currentField;
    private RenderTexture nextField;
    private int kernelHandle;
    private uint threadGroupSizeX;
    private uint threadGroupSizeY;
    private uint threadGroupSizeZ;
    private int FrameIndex = 0;
    private int EmissionFrameIndex = -1;

    private Material renderMaterial;
    
    public SensorsOverlay SensorsOverlay;
    
    void Start()
    {
        

        currentField = new(SimulationState.GRID_RES, SimulationState.GRID_RES, 0, RenderTextureFormat.RGFloat);
        currentField.enableRandomWrite = true;
        currentField.Create();
        nextField = new(SimulationState.GRID_RES, SimulationState.GRID_RES, 0, RenderTextureFormat.RGFloat);
        nextField.enableRandomWrite = true;
        nextField.Create();
        
        // Get kernel and thread group sizes
        kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);

        // Set initial textures
        computeShader.SetTexture(kernelHandle, "CurrentField", currentField);
        computeShader.SetTexture(kernelHandle, "NextField", nextField); 
        
         renderMaterial = GetComponent<MeshRenderer>().material;
        // Set the texture on the material
        renderMaterial.SetTexture("_CurrentField", currentField);

        computeShader.SetInt("SIM_RES_X", SimulationState.GRID_RES);
        computeShader.SetInt("SIM_RES_Y", SimulationState.GRID_RES);
        computeShader.SetFloat("DX", SimulationState.DX);
        computeShader.SetFloat("DT", SimulationState.DT);
        
        FrameIndex = 0;
        fps_frameTimes = new float[fps_frameBufferSize];
    }

    private float LastEmittedTime = 1;
    private bool Emitting = false;
    
    void Update()
    {
        bool emit = Input.GetKeyDown(KeyCode.Space) && LastEmittedTime > 1f;
        if (emit)
        {
            LastEmittedTime = 0;
            EmissionFrameIndex = 0;
             Emitting = true;
        }
        LastEmittedTime += Time.deltaTime;
        if (Emitting && EmissionFrameIndex++ > 100)
        {
            EmissionFrameIndex = -1;
            Emitting = false;
        }
        computeShader.SetInt("EmissionFrameIndex", EmissionFrameIndex);
        computeShader.SetInt("FrameIndex", FrameIndex);
        computeShader.SetBool("Emit", Emitting);
        computeShader.SetVector("EmitterPosition", SimulationState.emitterPosition);

        int groupsX = Mathf.CeilToInt(SimulationState.GRID_RES / (float)threadGroupSizeX);
        int groupsY = Mathf.CeilToInt(SimulationState.GRID_RES / (float)threadGroupSizeY);
        computeShader.Dispatch(kernelHandle, groupsX, groupsY, 1);

        // Swap render textures
        (currentField, nextField) = (nextField, currentField);

        // Update textures in compute shader
        computeShader.SetTexture(kernelHandle, "CurrentField", currentField);
        computeShader.SetTexture(kernelHandle, "NextField", nextField);

        // Update texture on material
        renderMaterial.SetTexture("_CurrentField", currentField);

        FrameIndex++;
        
        fps_frameTimes[FrameIndex % fps_frameBufferSize] = Time.deltaTime;
        float averageDeltaTime = 0f;
        foreach (float deltaTime in fps_frameTimes)
        {
            averageDeltaTime += deltaTime;
        }
        averageDeltaTime /= fps_frameBufferSize;
        float smoothedFPS = 1f / averageDeltaTime;
        fpsText.text = $"FPS: {Mathf.RoundToInt(smoothedFPS)}\nFrame: {FrameIndex}";
        //Debug.Log("Smoothed FPS: " + Mathf.RoundToInt(smoothedFPS));
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Saving data...");
            SensorsOverlay.SaveData();
        }
    }

    void OnDestroy()
    {
        if (currentField) currentField.Release();
        if (nextField) nextField.Release();
    }

    public RenderTexture GetSimulationField()
    {
        return currentField;
    }
}