using Starfire.Core.V2.World;

namespace StarfireV2
{
    /// <summary>
    /// Abstract noise field that can be sampled at any position.
    /// Supports composable noise algorithms for procedural world generation.
    /// </summary>
    public interface INoiseField
    {
        /// <summary>
        /// Sample the noise field at an absolute world position.
        /// </summary>
        float Sample(Vector2D position);

        /// <summary>
        /// Sample with additional seed for world-specific variation.
        /// </summary>
        float Sample(Vector2D position, float seed);
    }
}
