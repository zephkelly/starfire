using UnityEngine;
using StarfireV2;

namespace Starfire.Core.V2.World.Generation.Generators
{
    [CreateAssetMenu(fileName = "CelestialBodyGenerationConfig", menuName = "Starfire/World Generation/Celestial Body Generation Config")]
    public class CelestialBodyGenerationConfig : ScriptableObject
    {
        [Header("General")]
        public bool enabled = true;

        [Header("Search")]
        [Tooltip("Extra search margin beyond chunk bounds to find bodies whose influence overlaps the chunk")]
        public float searchMargin = 50000f;

        [Header("Visuals")]
        [Tooltip("Reference to the fabric-level celestial body config (contains star/planet type prefabs)")]
        public CelestialBodyConfig fabricConfig;
    }
}
