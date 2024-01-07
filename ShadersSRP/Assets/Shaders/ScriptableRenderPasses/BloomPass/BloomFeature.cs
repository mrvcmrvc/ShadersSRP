using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BloomFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        [field:SerializeField] public Shader BloomShader { get; private set; }
        [field:SerializeField, Range(1, 16)] public int DownScaleCount { get; private set; } = 1;

        [field:Header("Threshold Settings")]
        [field:SerializeField, Range(1f, 10f)] public float Threshold { get; private set; } = 1;
        [field:SerializeField, Range(0f, 1f)] public float SoftThreshold { get; private set; } = 0.5f;
        [field:SerializeField, Range(0, 10)] public float Intensity { get; private set; } = 1;
    }

    public Settings BloomSettings = new();

    private BloomPass bloomPass;
    
    public override void Create()
    {
        if (BloomSettings.BloomShader == null)
            return;
        
        bloomPass = new BloomPass(BloomSettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (bloomPass == null)
            return;
        
        renderer.EnqueuePass(bloomPass); 
    }
}