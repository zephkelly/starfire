using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// Render pass that draws objects on specified layers after post-processing.
    /// Uses DrawRenderers to render all visible renderers on the configured layer mask.
    /// </summary>
    public class ShipOverlayPass : ScriptableRenderPass
    {
        private LayerMask _layerMask;
        private FilteringSettings _filteringSettings;
        private readonly List<ShaderTagId> _shaderTagIds;

        private const string PassName = "Ship Overlay";

        public ShipOverlayPass(LayerMask layerMask)
        {
            _layerMask = layerMask;

            // Shader tags for 2D sprites (both lit and unlit)
            _shaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("Universal2D"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("Sprite")
            };

            // Filter to only render objects on the specified layer
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
        }

        public void UpdateLayerMask(LayerMask newMask)
        {
            _layerMask = newMask;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, newMask);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_layerMask == 0) return;

            var cmd = CommandBufferPool.Get(PassName);

            // Setup sorting - use the same sorting as normal 2D rendering
            var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };

            // Create drawing settings with shader tags for 2D sprites
            var drawingSettings = new DrawingSettings(_shaderTagIds[0], sortingSettings)
            {
                perObjectData = PerObjectData.None
            };

            // Add additional shader tags
            for (int i = 1; i < _shaderTagIds.Count; i++)
            {
                drawingSettings.SetShaderPassName(i, _shaderTagIds[i]);
            }

            // Draw all renderers on the specified layer
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
