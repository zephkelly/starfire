namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Defines how contacts outside the minimap display range are handled.
    /// </summary>
    public enum MinimapEdgeBehavior
    {
        /// <summary>
        /// Contacts outside range are not shown.
        /// </summary>
        HardCutoff,

        /// <summary>
        /// Contacts outside range are clamped to edge with directional indicator.
        /// </summary>
        ClampToEdge,

        /// <summary>
        /// Contacts fade out as they approach and exceed the edge.
        /// </summary>
        FadeAtEdge
    }
}
