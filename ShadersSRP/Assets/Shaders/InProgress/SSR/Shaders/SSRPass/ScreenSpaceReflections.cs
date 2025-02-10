using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceReflections : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Shader SSRShader;
    }

    public Settings SSRSettings = new();
    
    private SSRPass_BlitSSR blitSSRPass;
    private SSRPass_BlitToCam blitCamPass;

    private Material ssrMaterial;

    public override void Create()
    {
        if (SSRSettings.SSRShader == null)
        {
            return;
        }

        ssrMaterial = new Material(SSRSettings.SSRShader);
        blitSSRPass = new SSRPass_BlitSSR(SSRSettings, ssrMaterial);
        blitCamPass = new SSRPass_BlitToCam(SSRSettings);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (Application.isPlaying)
        {
            Destroy(ssrMaterial);
            return;
        }
        
        DestroyImmediate(ssrMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (blitSSRPass == null || blitCamPass == null || renderingData.cameraData.cameraType != CameraType.Game)
            return;
        
        renderer.EnqueuePass(blitSSRPass);
        renderer.EnqueuePass(blitCamPass);
    }
}