using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurDownScalePass : ScriptableRenderPass
{
    private const string profilerTag = "Blur Pass";

    private BlurRendererFeature.Settings settings;
    private CommandBuffer blurCommandBuffer;
    private RTHandle colorTarget;
    private Material blurMaterial;
    private RTHandle[] rtHandles;

    private const int downScalePass = 0;

    private static readonly int originalTextureId = Shader.PropertyToID("_OriginalTexture");
    private static readonly int intensityId = Shader.PropertyToID("_Intensity");
    private static readonly int texelOffsetId = Shader.PropertyToID("_TexelOffset");

    public BlurDownScalePass(BlurRendererFeature.Settings settings, Material material, RTHandle[] rtHandles)
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
        blurMaterial.SetVector(texelOffsetId, settings.DownScaleTexelOffset);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        blurCommandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(blurCommandBuffer, new ProfilingSampler(profilerTag)))
        {
            blurCommandBuffer.SetGlobalTexture(originalTextureId, colorTarget.rt);

            DownScale();

            //BlitBlurredTextureToBuffer();
        }

        context.ExecuteCommandBuffer(blurCommandBuffer);
        blurCommandBuffer.Clear();
        CommandBufferPool.Release(blurCommandBuffer);
    }

    private void DownScale()
    {
        //rtHandles[0] = colorTarget;
        RTHandle src = colorTarget;

        for (int currentIterationIndex = 0; currentIterationIndex < settings.DownScaleCount; currentIterationIndex++)
        {
            RTHandle dest = rtHandles[currentIterationIndex];

            Blitter.BlitTexture(blurCommandBuffer, src, dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, blurMaterial, downScalePass);

            src = dest;
        }
    }

    private void BlitBlurredTextureToBuffer()
    {
        Blitter.BlitCameraTexture(blurCommandBuffer, rtHandles[settings.DownScaleCount - 1], colorTarget, bilinear: true);
    }
}