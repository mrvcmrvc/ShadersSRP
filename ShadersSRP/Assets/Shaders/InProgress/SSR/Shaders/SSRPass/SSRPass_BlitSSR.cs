using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class SSRPass_BlitSSR : ScriptableRenderPass
{
    private Material ssrMaterial;

    private class PassData
    {
        public TextureHandle FromTexture;
    }
    
    public class CustomData : ContextItem
    {
        public TextureHandle SSRTexture;

        public override void Reset()
        {
            SSRTexture = TextureHandle.nullHandle;
        }
    }
    
    public SSRPass_BlitSSR(ScreenSpaceReflections.Settings settings, Material ssrMaterial)
    {
        renderPassEvent = settings.RenderPassEvent;
        this.ssrMaterial = ssrMaterial;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            passData.FromTexture = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
                desc, "_SSRTexture", false);
            
            CustomData customData = frameData.Create<CustomData>();
            customData.SSRTexture = destination;
            
            builder.UseTexture(passData.FromTexture);
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
            
            if (!passData.FromTexture.IsValid() || !destination.IsValid())
                return;
            
            builder.SetRenderFunc((PassData data, RasterGraphContext context)
                => ExecutePass(data, context));
        }
    }
    
    private void ExecutePass(PassData passData, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, passData.FromTexture, new Vector4(1, 1, 0, 0), ssrMaterial, 0);
    }
}

public class SSRPass_BlitToCam : ScriptableRenderPass
{
    private Material defaultBlitMaterial = Blitter.GetBlitMaterial(TextureDimension.Any);

    private class PassData
    {
        public TextureHandle FromTexture;
    }
    
    public SSRPass_BlitToCam(ScreenSpaceReflections.Settings settings)
    {
        renderPassEvent = settings.RenderPassEvent;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            SSRPass_BlitSSR.CustomData customData = frameData.Get<SSRPass_BlitSSR.CustomData>();
            
            passData.FromTexture = customData.SSRTexture;

            builder.UseTexture(passData.FromTexture);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            
            if (!passData.FromTexture.IsValid() || !resourceData.activeColorTexture.IsValid())
                return;
            
            builder.SetRenderFunc((PassData data, RasterGraphContext context)
                => ExecutePass(data, context));
        }
    }
    
    private void ExecutePass(PassData passData, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, passData.FromTexture, new Vector4(1, 1, 0, 0), defaultBlitMaterial, 0);
    }
}