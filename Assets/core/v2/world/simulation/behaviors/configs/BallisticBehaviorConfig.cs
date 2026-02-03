using UnityEngine;
using Starfire.Core.V2.World.Simulation.Behaviors.Runtime;

namespace Starfire.Core.V2.World.Simulation.Behaviors.Configs
{
    /// <summary>
    /// Configuration for ballistic (inertia + drag) behavior.
    /// This is the simplest behavior - entities coast along their current velocity
    /// with optional drag. Used for asteroids, debris, unpowered ships, and simple projectiles.
    /// </summary>
    [CreateAssetMenu(fileName = "BallisticBehavior", menuName = "Starfire/Simulation/Behaviors/Ballistic")]
    public class BallisticBehaviorConfig : SimulationBehaviorConfig
    {
        [Header("Ballistic Settings")]
        [Tooltip("If true, use the drag value from the entity. If false, use DefaultDrag.")]
        public bool UseEntityDrag = true;

        [Tooltip("Default drag coefficient when UseEntityDrag is false.")]
        [Min(0f)]
        public float DefaultDrag = 0f;

        [Tooltip("Minimum velocity magnitude below which entity is considered stopped.")]
        [Min(0f)]
        public float MinVelocityThreshold = 0.01f;

        [Tooltip("If true, stop the entity completely when velocity drops below threshold.")]
        public bool StopAtThreshold = true;

        [Header("Rotation")]
        [Tooltip("If true, apply angular velocity to rotation.")]
        public bool ApplyAngularVelocity = true;

        [Tooltip("Angular drag coefficient for rotation.")]
        [Min(0f)]
        public float AngularDrag = 0f;

        protected override void OnValidate()
        {
            base.OnValidate();
            BehaviorType = SimulatedBehaviorType.Ballistic;
            if (string.IsNullOrEmpty(BehaviorName))
            {
                BehaviorName = "Ballistic";
            }
        }

        public override ISimulationBehavior CreateBehavior()
        {
            return new BallisticBehavior(this);
        }
    }
}
