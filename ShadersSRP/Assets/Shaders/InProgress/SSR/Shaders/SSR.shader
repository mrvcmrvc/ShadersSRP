Shader "Custom/SSR"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _CameraDepthTexture ("Depth Texture", 2D) = "black" {}
        _CameraNormalsTexture ("Normals Texture", 2D) = "black" {}
        _ReflectionIntensity ("Reflection Intensity", Range(0, 1)) = 0.5
        _MaxSteps ("Max Ray Steps", Range(10, 100)) = 50
        _StepSize ("Step Size", Range(0.01, 1)) = 0.1
        _DepthTolerance ("Depth Tolerance", Range(0.001, 0.01)) = 0.001
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalsTexture;
            float _ReflectionIntensity;
            int _MaxSteps;
            float _StepSize;
            float _DepthTolerance;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Function to reconstruct world position from depth
            float3 ReconstructWorldPosition(float2 uv, float depth)
            {
                float4 clipPos = float4(uv * 2 - 1, depth, 1);
                float4 viewPos = mul(unity_CameraInvProjection, clipPos);
                return viewPos.xyz / viewPos.w;
            }

            // Screen-space reflections via raymarching
            float4 frag(v2f i) : SV_Target
            {
                float3 normal = tex2D(_CameraNormalsTexture, i.uv).rgb * 2 - 1;
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                float3 worldPos = ReconstructWorldPosition(i.uv, depth);
                float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos);
                float3 reflectDir = reflect(viewDir, normal);

                float2 rayUV = i.uv;
                rayUV += reflectDir.xy * _StepSize;
                rayUV += reflectDir.xy * _StepSize;
                rayUV += reflectDir.xy * _StepSize;
                rayUV += reflectDir.xy * _StepSize;
                rayUV += reflectDir.xy * _StepSize;
                rayUV += reflectDir.xy * _StepSize;
                
                float4 reflectionColor = tex2D(_MainTex, rayUV);
                float4 originalColor = tex2D(_MainTex, i.uv);
                return lerp(originalColor, reflectionColor, _ReflectionIntensity);
            }

            ENDHLSL
        }
    }
}
