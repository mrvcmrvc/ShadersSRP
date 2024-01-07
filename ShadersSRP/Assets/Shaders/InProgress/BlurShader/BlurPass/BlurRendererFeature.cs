using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [field: SerializeField] public Shader BlurShader { get; private set; }
        [field: SerializeField, Range(1, 9)] public int DownScaleCount { get; private set; } = 1;
        [field: SerializeField, Range(0.1f, 1f)] public float ResolutionScaling { get; private set; } = 0.5f;
        [field: SerializeField, Range(0f, 1f)] public float Intensity { get; private set; }
        [field: SerializeField] public Vector4 DownScaleTexelOffset { get; private set; }
        [field: SerializeField] public Vector4 UpScaleTexelOffset { get; private set; }
    }

    public Settings BlurSettings = new();

    private BlurDownScalePass blurDownScalePass;
    private BlurUpScalePass blurUpScalePass;
    private Material blurMaterial;

    private Vector2Int startingResolution;
    private RTHandle[] rtHandles;
    private RTHandleSystem rtHandleSystem;

    public override void Create()
    {
        if (BlurSettings.BlurShader == null)
            return;

        startingResolution = new Vector2Int(
            Mathf.RoundToInt(1920 * BlurSettings.ResolutionScaling),
            Mathf.RoundToInt(1080 * BlurSettings.ResolutionScaling));

        rtHandleSystem = new RTHandleSystem();
        rtHandleSystem.Initialize(startingResolution.x, startingResolution.y);

        AllocRTHandles();

        blurMaterial = CoreUtils.CreateEngineMaterial(BlurSettings.BlurShader);

        blurDownScalePass = new BlurDownScalePass(BlurSettings, blurMaterial, rtHandles);
        blurUpScalePass = new BlurUpScalePass(BlurSettings, blurMaterial, rtHandles, rtHandleSystem);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(blurMaterial);

        if (rtHandles != null)
        {
            foreach (RTHandle rTHandle in rtHandles)
            {
                rtHandleSystem.Release(rTHandle);
            }

            rtHandles = null;
        }

        rtHandleSystem.Dispose();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (blurDownScalePass == null || renderingData.cameraData.cameraType != CameraType.Game)
            return;

        renderer.EnqueuePass(blurDownScalePass);
        //renderer.EnqueuePass(blurUpScalePass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
                                    in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
            // ensures that the opaque texture is available to the Render Pass.
            blurDownScalePass.ConfigureInput(ScriptableRenderPassInput.Color);
            blurDownScalePass.SetTarget(renderer.cameraColorTargetHandle);

            blurUpScalePass.ConfigureInput(ScriptableRenderPassInput.Color);
            blurUpScalePass.SetTarget(renderer.cameraColorTargetHandle);
        }
    }

    private void AllocRTHandles()
    {
        rtHandles = new RTHandle[BlurSettings.DownScaleCount];

        int width = startingResolution.x;
        int height = startingResolution.y;

        for (int currentIterationIndex = 0; currentIterationIndex < BlurSettings.DownScaleCount; currentIterationIndex++)
        {
            width >>= 1;
            height >>= 1;

            if (width < 2 || height < 2)
                break;

            RTHandle newRTHandle = AllocNewRTHandle(width, height, $"RTHandle_{currentIterationIndex}");
            rtHandles[currentIterationIndex] = newRTHandle;
        }
    }

    private RTHandle AllocNewRTHandle(int width, int height, string name)
    {
        return rtHandleSystem.Alloc(width, height,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, filterMode: FilterMode.Bilinear,
            name: name, msaaSamples: MSAASamples.None);
    }
}
