using UnityEngine;

namespace StarfireV2.Fluid
{
    /// <summary>
    /// Configuration for the fluid wake simulation parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "FluidSimulationConfig", menuName = "Starfire/Fluid/Simulation Config")]
    public class FluidSimulationConfig : ScriptableObject
    {
        [Header("Simulation Quality")]
        [Tooltip("Resolution of the simulation grid (256 recommended for performance)")]
        [Range(64, 512)]
        public int resolution = 256;

        [Tooltip("World-space size the simulation covers (centered on camera)")]
        [Range(50f, 500f)]
        public float simulationWorldSize = 100f;

        [Header("Physics")]
        [Tooltip("Fluid viscosity (higher = thicker fluid, slower spread)")]
        [Range(0f, 1f)]
        public float viscosity = 0.1f;

        [Tooltip("How quickly density fades back (higher = faster recovery)")]
        [Range(0f, 5f)]
        public float densityDecay = 0.5f;

        [Tooltip("How quickly velocity dampens")]
        [Range(0f, 10f)]
        public float velocityDamping = 2f;

        [Tooltip("Number of pressure solve iterations (more = accurate, slower)")]
        [Range(10, 50)]
        public int pressureIterations = 20;

        [Tooltip("Number of diffusion iterations (skip if viscosity is low)")]
        [Range(0, 10)]
        public int diffuseIterations = 4;

        [Header("Ship Interaction")]
        [Tooltip("How strongly ships push gas outward")]
        [Range(0f, 100f)]
        public float shipPushStrength = 30f;

        [Tooltip("Radius multiplier for ship influence zone")]
        [Range(1f, 10f)]
        public float influenceRadiusMultiplier = 3f;

        [Header("Density")]
        [Tooltip("Initial/ambient density value (1 = fully opaque gas)")]
        [Range(0f, 1f)]
        public float initialDensity = 1f;

        [Tooltip("How quickly displaced gas returns (0 = never)")]
        [Range(0f, 1f)]
        public float densitySourceStrength = 0.2f;

        [Header("Performance")]
        [Tooltip("Skip simulation when no obstacles are active")]
        public bool skipWhenNoObstacles = true;

        [Tooltip("Minimum ship speed to trigger simulation (0 = no filtering)")]
        [Range(0f, 10f)]
        public float minimumSpeedThreshold = 0f;

        [Tooltip("Distance from camera at which ships stop affecting fluid")]
        [Range(20f, 200f)]
        public float maxObstacleDistance = 80f;

        [Tooltip("Only simulate when ships are inside nebula regions (disable for testing)")]
        public bool requireNebulaPresence = false;
    }
}
