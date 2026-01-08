using UnityEngine;

namespace Starfire.Core.Background.Behaviors
{
    /// <summary>
    /// Lightweight runtime data for behavior-specific parameters.
    /// Stored per-star, used for opacity calculations.
    /// </summary>
    [System.Serializable]
    public struct ShootingStarBehaviorData
    {
        public ShootingStarBehaviorType behaviorType;

        /// <summary>
        /// Behavior-specific parameter 1:
        /// Standard: fadeInEnd, Persistent: exitFadeDuration, SlowFade: fadeDuration/fadeDistance
        /// </summary>
        public float fadeParam1;

        /// <summary>
        /// Behavior-specific parameter 2:
        /// Standard: fadeOutStart, Persistent: exitGracePeriod, SlowFade: fadeDelay
        /// </summary>
        public float fadeParam2;

        /// <summary>
        /// Behavior-specific parameter 3:
        /// SlowFade: fadeMode (0=time, 1=distance)
        /// </summary>
        public float fadeParam3;

        /// <summary>
        /// Bit flags for runtime state.
        /// Bit 0: hasEnteredView, Bit 1: isExitFading
        /// </summary>
        public byte stateFlags;

        public bool HasEnteredView => (stateFlags & 0x01) != 0;
        public bool IsExitFading => (stateFlags & 0x02) != 0;

        public void SetHasEnteredView() => stateFlags |= 0x01;
        public void SetIsExitFading() => stateFlags |= 0x02;

        /// <summary>
        /// Create default Standard behavior data.
        /// </summary>
        public static ShootingStarBehaviorData CreateDefault()
        {
            return new ShootingStarBehaviorData
            {
                behaviorType = ShootingStarBehaviorType.Standard,
                fadeParam1 = 0.1f,  // fadeInEnd
                fadeParam2 = 0.9f,  // fadeOutStart
                fadeParam3 = 0f,
                stateFlags = 0
            };
        }
    }

    /// <summary>
    /// Base configuration for shooting star behaviors.
    /// Each behavior type has its own derived config with type-specific parameters.
    /// </summary>
    public abstract class ShootingStarBehaviorConfig : ScriptableObject
    {
        [Header("Behavior Identity")]
        [SerializeField] protected string behaviorId = "behavior";
        [SerializeField] protected string displayName = "Behavior";

        [Header("Selection Weight")]
        [Tooltip("Weight for random selection (higher = more likely)")]
        [Min(0.01f)]
        [SerializeField] protected float selectionWeight = 1f;

        public string BehaviorId => behaviorId;
        public string DisplayName => displayName;
        public float SelectionWeight => selectionWeight;

        /// <summary>
        /// The type of behavior this config represents.
        /// </summary>
        public abstract ShootingStarBehaviorType BehaviorType { get; }

        /// <summary>
        /// Create runtime behavior data for a new star.
        /// </summary>
        public abstract ShootingStarBehaviorData CreateBehaviorData();
    }
}
