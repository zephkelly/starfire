using UnityEngine;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Interface for minimap rendering strategies.
    /// Allows different display types (circular, rectangular, etc.).
    /// </summary>
    public interface IMinimapRenderer
    {
        /// <summary>
        /// Whether the renderer is initialized and ready.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// The effective display range in world units.
        /// </summary>
        float DisplayRange { get; }

        /// <summary>
        /// Container transform for blip elements.
        /// </summary>
        RectTransform BlipContainer { get; }

        /// <summary>
        /// Initialize the renderer with configuration.
        /// </summary>
        void Initialize(MinimapConfig config, RectTransform container);

        /// <summary>
        /// Update the display range (e.g., when sensor range changes).
        /// </summary>
        void SetDisplayRange(float range);

        /// <summary>
        /// Convert world-relative position to minimap UI position.
        /// </summary>
        /// <param name="relativeWorldPos">Position relative to the source (player)</param>
        /// <param name="sourceRotation">Source entity rotation in degrees</param>
        /// <param name="orientation">Current orientation mode</param>
        /// <returns>Position in minimap local coordinates</returns>
        Vector2 WorldToMinimapPosition(
            Vector2 relativeWorldPos,
            float sourceRotation,
            MinimapOrientationMode orientation);

        /// <summary>
        /// Check if a minimap position is within the displayable area.
        /// </summary>
        bool IsInBounds(Vector2 minimapPos);

        /// <summary>
        /// Apply edge behavior to an out-of-bounds position.
        /// </summary>
        /// <param name="minimapPos">Original minimap position</param>
        /// <param name="behavior">Edge behavior to apply</param>
        /// <param name="edgeFactor">Output: 0-1 factor for fading (1 = fully visible)</param>
        /// <returns>Adjusted position</returns>
        Vector2 ApplyEdgeBehavior(
            Vector2 minimapPos,
            MinimapEdgeBehavior behavior,
            out float edgeFactor);

        /// <summary>
        /// Update the player icon rotation (for NorthUp mode).
        /// </summary>
        void UpdatePlayerRotation(float rotation);

        /// <summary>
        /// Update sweep effect and other time-based visuals.
        /// </summary>
        void UpdateVisuals(float deltaTime, float sensorPollingRate);

        /// <summary>
        /// Show/hide stale data indicator.
        /// </summary>
        void SetStaleIndicatorVisible(bool visible);

        /// <summary>
        /// Clean up renderer resources.
        /// </summary>
        void Dispose();
    }
}
