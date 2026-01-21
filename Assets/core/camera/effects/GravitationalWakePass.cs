using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// Render pass that applies the gravitational wake distortion effect using RenderGraph.
    /// Blits the scene color with the wake distortion shader.
    /// </summary>
    public class GravitationalWakePass : ScriptableRenderPass
    {
        private Material _wakeMaterial;
        private float _activationThreshold;

        private static readonly int WarpIntensityId = Shader.PropertyToID("_WarpIntensity");
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");

        private const string PassName = "Gravitational Wake";

        public void Setup(Material material, float activationThreshold)
        {
            _wakeMaterial = material;
            _activationThreshold = activationThreshold;
            requiresIntermediateTexture = true;
        }

        private class PassData
        {
            public Material WakeMaterial;
            public TextureHandle SourceTexture;
            public TextureHandle DestinationTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Early exit check - this should already be caught by the feature, but double-check
            float currentIntensity = Shader.GetGlobalFloat(WarpIntensityId);
            if (currentIntensity < _activationThreshold || _wakeMaterial == null)
            {
                return;
            }

            var resourceData = frameData.Get<UniversalResourceData>();

            // Get the active color texture
            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle destination = resourceData.cameraColor;

            if (!source.IsValid() || !destination.IsValid())
            {
                return;
            }

            // Create a temporary texture to copy source into
            var sourceDesc = renderGraph.GetTextureDesc(source);
            sourceDesc.name = "_GravitationalWakeTemp";
            sourceDesc.clearBuffer = false;
            TextureHandle tempTexture = renderGraph.CreateTexture(sourceDesc);

            // First pass: Copy source to temp
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName + "_Copy", out var copyData))
            {
                copyData.SourceTexture = source;
                copyData.DestinationTexture = tempTexture;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.SourceTexture, new Vector4(1, 1, 0, 0), 0, false);
                });
            }

            // Second pass: Apply effect from temp to camera color
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
            {
                passData.WakeMaterial = _wakeMaterial;
                passData.SourceTexture = tempTexture;
                passData.DestinationTexture = destination;

                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Set the source texture for the shader
                    data.WakeMaterial.SetTexture(BlitTextureId, data.SourceTexture);
                    Blitter.BlitTexture(context.cmd, data.SourceTexture, new Vector4(1, 1, 0, 0), data.WakeMaterial, 0);
                });
            }
        }

        public void Dispose()
        {
            // No manual cleanup needed with RenderGraph - textures are managed automatically
        }
    }
}
