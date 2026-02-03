using UnityEngine;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Interface for entity definitions used by the background simulation system.
    /// Implementations wrap type-specific definitions (AsteroidDefinition, ShipDefinition, etc.)
    /// to provide a common interface for chunk modification tracking and entity matching.
    /// </summary>
    public interface ISimulatableDefinition
    {
        /// <summary>Type of entity this definition represents.</summary>
        EntityType EntityType { get; }

        /// <summary>Local position within the chunk (relative to chunk center).</summary>
        Vector2 LocalPosition { get; }

        /// <summary>Size/radius of the entity.</summary>
        float Size { get; }

        /// <summary>Rotation in degrees.</summary>
        float Rotation { get; }

        /// <summary>
        /// Check if this definition matches another definition within tolerance.
        /// Used for identifying procedural entities that have been modified.
        /// </summary>
        /// <param name="other">The definition to compare against.</param>
        /// <param name="positionTolerance">Position tolerance for matching.</param>
        /// <returns>True if the definitions match.</returns>
        bool Matches(ISimulatableDefinition other, float positionTolerance = 0.01f);

        /// <summary>
        /// Generate a hash for O(1) lookup based on position.
        /// Position is quantized to ensure stable hashing.
        /// </summary>
        /// <param name="quantizationScale">Scale factor for position quantization (default 100 = 0.01 unit precision).</param>
        /// <returns>A stable hash based on entity type and quantized position.</returns>
        long GetPositionHash(float quantizationScale = 100f);

        /// <summary>
        /// Serialize type-specific data for save/restore.
        /// </summary>
        /// <returns>Serialized type-specific data.</returns>
        byte[] SerializeTypeData();

        /// <summary>
        /// Deserialize type-specific data from save file.
        /// </summary>
        /// <param name="data">Serialized type-specific data.</param>
        void DeserializeTypeData(byte[] data);
    }
}
