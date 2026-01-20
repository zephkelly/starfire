using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// URP Renderer Feature that applies directional chromatic aberration along the warp direction.
    /// Reads _WarpIntensity and _WarpDirection globals set by WarpEffectController.
    /// </summary>
    public class DirectionalChromaticAberrationFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("When to inject this pass in the render pipeline")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

            [Tooltip("Material using the DirectionalChromaticAberration shader")]
            public Material material;

            [Tooltip("Intensity multiplier for the chromatic offset")]
            [Range(0f, 1f)]
            public float intensity = 0.5f;

            [Tooltip("Maximum pixel offset for RGB channel separation")]
            [Range(0f, 50f)]
            public float pixelOffset = 15f;
        }

        public Settings settings = new Settings();
        private DirectionalChromaticPass _pass;

        public override void Create()
        {
            _pass = new DirectionalChromaticPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material == null)
            {
                return;
            }

            // Only apply when camera is a game camera
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
        }

        class DirectionalChromaticPass : ScriptableRenderPass
        {
            private readonly Settings _settings;
            private RTHandle _tempTexture;
            private static readonly int ChromaticIntensityId = Shader.PropertyToID("_ChromaticIntensity");
            private static readonly int PixelOffsetId = Shader.PropertyToID("_PixelOffset");

            public DirectionalChromaticPass(Settings settings)
            {
                _settings = settings;
                renderPassEvent = settings.renderPassEvent;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _tempTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_DirectionalChromaticTemp");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_settings.material == null)
                {
                    return;
                }

                // Skip if warp intensity is zero (no point processing)
                float warpIntensity = Shader.GetGlobalFloat("_WarpIntensity");
                if (warpIntensity < 0.001f)
                {
                    return;
                }

                var cmd = CommandBufferPool.Get("Directional Chromatic Aberration");

                // Set material properties
                _settings.material.SetFloat(ChromaticIntensityId, _settings.intensity);
                _settings.material.SetFloat(PixelOffsetId, _settings.pixelOffset);

                // Get camera color target
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Blit through the shader
                Blitter.BlitCameraTexture(cmd, source, _tempTexture, _settings.material, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture, source);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }

            public void Dispose()
            {
                _tempTexture?.Release();
            }
        }
    }
}
