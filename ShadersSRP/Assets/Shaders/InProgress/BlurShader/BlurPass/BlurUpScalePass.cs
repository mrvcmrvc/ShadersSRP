using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurUpScalePass : ScriptableRenderPass
{
    private const string profilerTag = "Blur Pass";

    private BlurRendererFeature.Settings settings;
    private CommandBuffer blurCommandBuffer;
    private RTHandle colorTarget;
    private Material blurMaterial;
    private RTHandle[] rtHandles;

    private const int upScalePass = 1;

    private static readonly int intensityId = Shader.PropertyToID("_Intensity");
    private static readonly int texelOffsetId = Shader.PropertyToID("_TexelOffset");

    public BlurUpScalePass(BlurRendererFeature.Settings settings, Material material, RTHandle[] rtHandles, RTHandleSystem rtHandleSystem)
    {
        this.settings = settings;
        this.rtHandles = rtHandles;

        renderPassEvent = settings.RenderPassEvent;
        blurMaterial = material;
    }

    public void SetTarget(RTHandle colorTarget)
    {
        this.colorTarget = colorTarget;

        UpdateShaderProperties();
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(colorTarget);
    }

    private void UpdateShaderProperties()
    {
        blurMaterial.SetFloat(intensityId, Mathf.GammaToLinearSpace(settings.Intensity));
        blurMaterial.SetVector(texelOffsetId, settings.UpScaleTexelOffset);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        blurCommandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(blurCommandBuffer, new ProfilingSampler(profilerTag)))
        {
            UpScale();

            BlitBlurredTextureToBuffer();
        }

        context.ExecuteCommandBuffer(blurCommandBuffer);
        blurCommandBuffer.Clear();
        CommandBufferPool.Release(blurCommandBuffer);
    }

    private void UpScale()
    {
        int currentIterationIndex = settings.DownScaleCount - 2;
        int previousDestinationIndex = settings.DownScaleCount - 1;

        for (; currentIterationIndex >= 0; currentIterationIndex--)
        {
            RTHandle src = rtHandles[previousDestinationIndex];
            RTHandle dest = rtHandles[currentIterationIndex];

            Blitter.BlitTexture(blurCommandBuffer, src, dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, blurMaterial, upScalePass);

            previousDestinationIndex = currentIterationIndex;
        }
    }

    private void BlitBlurredTextureToBuffer()
    {
        Blitter.BlitCameraTexture(blurCommandBuffer, rtHandles[0], colorTarget, bilinear: true);
    }
}