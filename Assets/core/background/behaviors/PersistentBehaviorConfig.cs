using UnityEngine;

namespace Starfire.Core.Background.Behaviors
{
    /// <summary>
    /// Persistent behavior: stays visible while in camera view.
    /// When leaving view, continues moving but starts slow fade.
    /// </summary>
    [CreateAssetMenu(fileName = "PersistentBehavior", menuName = "Starfire/Background/Behaviors/Persistent")]
    public class PersistentBehaviorConfig : ShootingStarBehaviorConfig
    {
        [Header("Persistent Behavior Settings")]
        [Tooltip("Duration of slow fade when exiting view (seconds)")]
        [Min(0.1f)]
        [SerializeField] private float exitFadeDuration = 2f;

        [Tooltip("Grace period after leaving view before fade starts (seconds)")]
        [Min(0f)]
        [SerializeField] private float exitGracePeriod = 0.5f;

        public float ExitFadeDuration => exitFadeDuration;
        public float ExitGracePeriod => exitGracePeriod;

        public override ShootingStarBehaviorType BehaviorType => ShootingStarBehaviorType.Persistent;

        public override ShootingStarBehaviorData CreateBehaviorData()
        {
            return new ShootingStarBehaviorData
            {
                behaviorType = ShootingStarBehaviorType.Persistent,
                fadeParam1 = exitFadeDuration,
                fadeParam2 = exitGracePeriod,
                fadeParam3 = 0f,
                stateFlags = 0
            };
        }
    }
}
