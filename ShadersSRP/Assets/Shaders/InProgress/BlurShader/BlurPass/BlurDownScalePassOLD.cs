using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurDownScalePassOLD : MonoBehaviour
{
    [SerializeField]
    private Shader blurShader;
    [SerializeField]
    private Camera renderCamera;
    [SerializeField, Range(1, 16)]
    private int downScaleCount = 2;
    [SerializeField, Range(0f, 1f)]
    private float resolutionScaling = 0.5f;

    private CommandBuffer blurCommandBuffer;
    private Material blurMaterial;
    private Vector2Int startingResolution;

    private const int downScalePass = 0;

    private static readonly int originalTextureId = Shader.PropertyToID("_OriginalTexture");
    private static readonly int intensityId = Shader.PropertyToID("_Intensity");

    private const CameraEvent CameraEvent = UnityEngine.Rendering.CameraEvent.AfterForwardAlpha;


    private void OnEnable()
    {
        if (blurCommandBuffer != null)
            return;

        Startup();

        RenderBlur();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void Startup()
    {
        Initialize();
    }

    private void Initialize()
    {
        blurCommandBuffer = new CommandBuffer
        {
            name = "BLUR"
        };

        blurMaterial = new Material(blurShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        startingResolution = new Vector2Int(
            Mathf.RoundToInt(Screen.width * resolutionScaling),
            Mathf.RoundToInt(Screen.height * resolutionScaling));
    }

    private void RenderBlur()
    {
        SetCameraRenderTextureAsGlobal();

        AddCameraRenderTextureToBuffer();

        DownScale();
        UpScale();

        BlitBlurredTextureToBuffer();

        renderCamera.AddCommandBuffer(CameraEvent, blurCommandBuffer);
        blurMaterial.SetFloat(intensityId, 1f);
    }

    private void Cleanup()
    {
        DestroyImmediate(blurMaterial);

        if (blurCommandBuffer == null)
            return;

        if (renderCamera != null)
            renderCamera.RemoveCommandBuffer(CameraEvent, blurCommandBuffer);

        blurCommandBuffer.Clear();
        blurCommandBuffer = null;
    }

    private void DownScale()
    {
        int width = startingResolution.x;
        int height = startingResolution.y;

        int dest = Shader.PropertyToID($"currentDestination_{0}");
        int src = dest;

        for (int currentIterationIndex = 1; currentIterationIndex <= downScaleCount; currentIterationIndex++)
        {
            width >>= 1;
            height >>= 1;

            if (width < 2 || height < 2)
                break;

            dest = Shader.PropertyToID($"currentDestination_{currentIterationIndex}");

            blurCommandBuffer.GetTemporaryRT(dest, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            blurCommandBuffer.Blit(src, dest, blurMaterial, downScalePass);

            src = dest;
        }
    }

    private void UpScale()
    {
        int currentIterationIndex = downScaleCount - 1;
        int previousDestination = downScaleCount;

        for (; currentIterationIndex >= 0; currentIterationIndex--)
        {
            int src = Shader.PropertyToID($"currentDestination_{previousDestination}");
            int dest = Shader.PropertyToID($"currentDestination_{currentIterationIndex}");

            blurCommandBuffer.Blit(src, dest, blurMaterial, 1);
            blurCommandBuffer.ReleaseTemporaryRT(src);

            previousDestination = currentIterationIndex;
        }
    }

    private void BlitBlurredTextureToBuffer()
    {
        int src = Shader.PropertyToID($"currentDestination_{0}");
        blurCommandBuffer.Blit(src, BuiltinRenderTextureType.CameraTarget);
    }

    private void AddCameraRenderTextureToBuffer()
    {
        int dest = Shader.PropertyToID($"currentDestination_{0}");
        blurCommandBuffer.GetTemporaryRT(dest, startingResolution.x, startingResolution.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        blurCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, dest, blurMaterial, downScalePass);
    }

    private void SetCameraRenderTextureAsGlobal()
    {
        blurCommandBuffer.SetGlobalTexture(originalTextureId, BuiltinRenderTextureType.CameraTarget);
    }
}