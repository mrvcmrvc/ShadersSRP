using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomPass : ScriptableRenderPass
{
    private const string profilerTag = "Bloom Pass";

    private BloomFeature.Settings settings;
    private Material bloomMaterial;
    private CommandBuffer bloomCommandBuffer;
    private RenderTargetIdentifier colorBuffer;

    private const int preFilterPass = 1;
    private const int downScalePass = 2;
    private const int upScalePass = 3;
    private const int bloomPass = 0;

    private static readonly int FILTER_ID = Shader.PropertyToID("_Filter");
    private static readonly int ORIGINAL_TEXTURE_ID = Shader.PropertyToID("_OriginalTexture");
    private static readonly int INTENSITY_ID = Shader.PropertyToID("_Intensity");

    public BloomPass(BloomFeature.Settings settings)
    {
        this.settings = settings;

        renderPassEvent = settings.RenderPassEvent; 
        
        bloomMaterial = new Material(settings.BloomShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        colorBuffer = renderingData.cameraData.renderer.cameraColorTargetHandle;
        
        UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {
        bloomMaterial.SetFloat(INTENSITY_ID, Mathf.GammaToLinearSpace(settings.Intensity));
        bloomMaterial.SetVector(FILTER_ID, GetFilter());
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        bloomCommandBuffer = CommandBufferPool.Get();
        using (new ProfilingScope(bloomCommandBuffer, new ProfilingSampler(profilerTag)))
        {
            bloomCommandBuffer.SetGlobalTexture(ORIGINAL_TEXTURE_ID, colorBuffer);

            DownScale();
            UpScale();

            BlitBlurredTextureToBuffer();
        }
                
        context.ExecuteCommandBuffer(bloomCommandBuffer);
        CommandBufferPool.Release(bloomCommandBuffer);
    }
    
    private Vector4 GetFilter()
    {
        float knee = settings.Threshold * settings.SoftThreshold;
        Vector4 filter;
        filter.x = settings.Threshold;
        filter.y = filter.x - knee;
        filter.z = 2f * knee;
        filter.w = 0.25f / (knee + 0.00001f);

        return filter;
    }
    
    private void DownScale()
    {
        int width = Screen.width;
        int height = Screen.height;
        
        int dest = Shader.PropertyToID($"currentDestination_{0}");
        bloomCommandBuffer.GetTemporaryRT(dest, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.BGRA32);
        //Blit(bloomCommandBuffer, BuiltinRenderTextureType.CameraTarget, dest, bloomMaterial, preFilterPass);
        
        int currentIterationIndex = 1;
        int src = dest;
        for (; currentIterationIndex < settings.DownScaleCount; currentIterationIndex++)
        {
            width >>= 1;
            height >>= 1;
            
            if(width < 2 || height < 2)
                break;
        
            dest = Shader.PropertyToID($"currentDestination_{currentIterationIndex}");
            
            bloomCommandBuffer.GetTemporaryRT(dest, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.BGRA32);
            //Blit(bloomCommandBuffer, src, dest, bloomMaterial, downScalePass);

            src = dest;
        }
    }
    
    private void UpScale()
    {
        int currentIterationIndex = settings.DownScaleCount - 2;
        int previousDestination = settings.DownScaleCount - 1;

        for (; currentIterationIndex >= 0; currentIterationIndex--)
        {
            int src = Shader.PropertyToID($"currentDestination_{previousDestination}");
            int dest = Shader.PropertyToID($"currentDestination_{currentIterationIndex}");

            //Blit(bloomCommandBuffer, src, dest, bloomMaterial, upScalePass);
            
            bloomCommandBuffer.ReleaseTemporaryRT(previousDestination);
            previousDestination = currentIterationIndex;
        }
    }
    
    private void BlitBlurredTextureToBuffer()
    {
        int src = Shader.PropertyToID($"currentDestination_{0}");
        bloomCommandBuffer.Blit(src, colorBuffer, bloomMaterial, bloomPass);
    }
}
