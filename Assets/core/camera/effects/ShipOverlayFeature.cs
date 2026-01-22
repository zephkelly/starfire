using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// URP Renderer Feature that renders specified layers AFTER post-processing.
    /// This allows entities like the player ship to be rendered on top of post-process
    /// effects (like gravitational wake distortion) without being affected by them.
    /// </summary>
    public class ShipOverlayFeature : ScriptableRendererFeature
    {
        [Header("Configuration")]
        [Tooltip("Layers to render after post-processing")]
        public LayerMask overlayLayers = 1 << 8; // Default to Player layer (index 8)

        [Tooltip("When to render the overlay (should be after post-processing)")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        private ShipOverlayPass _overlayPass;

        public override void Create()
        {
            _overlayPass = new ShipOverlayPass(overlayLayers)
            {
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Skip in scene view if desired (optional)
            // if (renderingData.cameraData.cameraType == CameraType.SceneView) return;

            // Only add the pass if we have layers to render
            if (overlayLayers != 0)
            {
                _overlayPass.UpdateLayerMask(overlayLayers);
                renderer.EnqueuePass(_overlayPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _overlayPass?.Dispose();
        }
    }
}
