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

        // Animation property IDs
        private static readonly int WakePulseSpeedId = Shader.PropertyToID("_WakePulseSpeed");
        private static readonly int WakePulseAmountId = Shader.PropertyToID("_WakePulseAmount");
        private static readonly int WakeRippleCountId = Shader.PropertyToID("_WakeRippleCount");
        private static readonly int WakeRippleSpeedId = Shader.PropertyToID("_WakeRippleSpeed");
        private static readonly int WakeRippleStrengthId = Shader.PropertyToID("_WakeRippleStrength");
        private static readonly int WakeNoiseScaleId = Shader.PropertyToID("_WakeNoiseScale");
        private static readonly int WakeNoiseSpeedId = Shader.PropertyToID("_WakeNoiseSpeed");
        private static readonly int WakeNoiseStrengthId = Shader.PropertyToID("_WakeNoiseStrength");
        private static readonly int WakeBowWaveStrengthId = Shader.PropertyToID("_WakeBowWaveStrength");

        // Edge distortion property IDs
        private static readonly int WakeEdgeStrengthId = Shader.PropertyToID("_WakeEdgeStrength");
        private static readonly int WakeEdgeSharpnessId = Shader.PropertyToID("_WakeEdgeSharpness");

        // Wake zone property IDs
        private static readonly int WakeAngleId = Shader.PropertyToID("_WakeAngle");
        private static readonly int WakeTurbulenceId = Shader.PropertyToID("_WakeTurbulence");
        private static readonly int WakeTurbulenceScaleId = Shader.PropertyToID("_WakeTurbulenceScale");
        private static readonly int WakeTurbulenceSpeedId = Shader.PropertyToID("_WakeTurbulenceSpeed");
        private static readonly int WakeSpreadId = Shader.PropertyToID("_WakeSpread");

        // Zoom scaling property ID
        private static readonly int WakeReferenceOrthoSizeId = Shader.PropertyToID("_WakeReferenceOrthoSize");

        // Ellipse shape property IDs
        private static readonly int WakeEllipseRatioId = Shader.PropertyToID("_WakeEllipseRatio");
        private static readonly int WakeNeedleSharpnessId = Shader.PropertyToID("_WakeNeedleSharpness");

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

            // Animation parameters (scaled by intensity)
            Shader.SetGlobalFloat(WakePulseSpeedId, config.pulseSpeed);
            Shader.SetGlobalFloat(WakePulseAmountId, config.pulseAmount * intensityMultiplier);
            Shader.SetGlobalFloat(WakeRippleCountId, config.rippleCount);
            Shader.SetGlobalFloat(WakeRippleSpeedId, config.rippleSpeed);
            Shader.SetGlobalFloat(WakeRippleStrengthId, config.rippleStrength * intensityMultiplier);
            Shader.SetGlobalFloat(WakeNoiseScaleId, config.noiseScale);
            Shader.SetGlobalFloat(WakeNoiseSpeedId, config.noiseSpeed);
            Shader.SetGlobalFloat(WakeNoiseStrengthId, config.noiseStrength * intensityMultiplier);
            Shader.SetGlobalFloat(WakeBowWaveStrengthId, config.bowWaveStrength * intensityMultiplier);

            // Edge distortion parameters
            Shader.SetGlobalFloat(WakeEdgeStrengthId, config.edgeDistortionStrength * intensityMultiplier);
            Shader.SetGlobalFloat(WakeEdgeSharpnessId, config.edgeSharpness);

            // Wake zone parameters
            Shader.SetGlobalFloat(WakeAngleId, config.wakeAngle);
            Shader.SetGlobalFloat(WakeTurbulenceId, config.wakeTurbulence * intensityMultiplier);
            Shader.SetGlobalFloat(WakeTurbulenceScaleId, config.wakeTurbulenceScale);
            Shader.SetGlobalFloat(WakeTurbulenceSpeedId, config.wakeTurbulenceSpeed);
            Shader.SetGlobalFloat(WakeSpreadId, config.wakeSpread);

            // Zoom scaling
            Shader.SetGlobalFloat(WakeReferenceOrthoSizeId, config.referenceOrthoSize);

            // Ellipse shape
            Shader.SetGlobalFloat(WakeEllipseRatioId, config.ellipseRatio);
            Shader.SetGlobalFloat(WakeNeedleSharpnessId, config.needleSharpness);
        }

        protected override void Dispose(bool disposing)
        {
            _wakePass?.Dispose();
        }
    }
}
