using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// URP Renderer Feature that applies a gravitational wake distortion effect.
    /// The effect creates a bubble of warped spacetime around the ship with a
    /// trailing wake effect behind it based on velocity direction.
    /// </summary>
    public class GravitationalWakeFeature : ScriptableRendererFeature
    {
        [Header("Configuration")]
        [Tooltip("Configuration asset for the wake effect")]
        public GravitationalWakeConfig config;

        [Tooltip("Material using the Starfire/GravitationalWake shader")]
        public Material wakeMaterial;

        [Tooltip("When in the render pipeline to apply the effect")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private GravitationalWakePass _wakePass;

        // Shader property IDs
        private static readonly int WarpIntensityId = Shader.PropertyToID("_WarpIntensity");
        private static readonly int WakeBubbleRadiusId = Shader.PropertyToID("_WakeBubbleRadius");
        private static readonly int WakeRingWidthId = Shader.PropertyToID("_WakeRingWidth");
        private static readonly int WakeTrailLengthId = Shader.PropertyToID("_WakeTrailLength");
        private static readonly int WakeDistortionStrengthId = Shader.PropertyToID("_WakeDistortionStrength");
        private static readonly int WakeTrailFalloffId = Shader.PropertyToID("_WakeTrailFalloff");
        private static readonly int WakeDirectionalBiasId = Shader.PropertyToID("_WakeDirectionalBias");
        private static readonly int WakeChromaStrengthId = Shader.PropertyToID("_WakeChromaStrength");

        public override void Create()
        {
            _wakePass = new GravitationalWakePass
            {
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Skip if no material or config assigned
            if (wakeMaterial == null || config == null)
            {
                return;
            }

            // Get current warp intensity
            float warpIntensity = Shader.GetGlobalFloat(WarpIntensityId);

            // Skip if warp intensity is below threshold (zero overhead when not warping)
            if (warpIntensity < config.activationThreshold)
            {
                return;
            }

            // Update shader globals from config
            UpdateShaderGlobals(warpIntensity);

            _wakePass.Setup(wakeMaterial, config.activationThreshold);
            renderer.EnqueuePass(_wakePass);
        }

        private void UpdateShaderGlobals(float warpIntensity)
        {
            // Calculate intensity-modulated distortion strength
            float intensityMultiplier = config.GetIntensityMultiplier(warpIntensity);
            float modulatedStrength = config.distortionStrength * intensityMultiplier;

            // Set all wake shader globals
            Shader.SetGlobalFloat(WakeBubbleRadiusId, config.bubbleRadius);
            Shader.SetGlobalFloat(WakeRingWidthId, config.ringWidth);
            Shader.SetGlobalFloat(WakeTrailLengthId, config.trailLength);
            Shader.SetGlobalFloat(WakeDistortionStrengthId, modulatedStrength);
            Shader.SetGlobalFloat(WakeTrailFalloffId, config.trailFalloff);
            Shader.SetGlobalFloat(WakeDirectionalBiasId, config.directionalBias);
            Shader.SetGlobalFloat(WakeChromaStrengthId, config.chromaEnabled ? config.chromaStrength * intensityMultiplier : 0f);
        }

        protected override void Dispose(bool disposing)
        {
            _wakePass?.Dispose();
        }
    }
}
