namespace Starfire.Core.Background.Regions
{
    /// <summary>
    /// Defines how a nebula region's edges are rendered.
    /// </summary>
    public enum NebulaEdgeBehavior
    {
        /// <summary>
        /// Gradual density decrease at edges (smooth falloff from center to boundary).
        /// </summary>
        SmoothFalloff = 0,

        /// <summary>
        /// Hard cutoff at the boundary with minimal transition.
        /// </summary>
        SharpBoundary = 1,

        /// <summary>
        /// Inverse falloff - creates a "clear zone" where nebula density is 0 inside
        /// and full outside. Useful for punching holes in nebula fields.
        /// </summary>
        InverseFalloff = 2
    }
}
