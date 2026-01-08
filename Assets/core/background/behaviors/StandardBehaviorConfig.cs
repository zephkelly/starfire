using UnityEngine;

namespace Starfire.Core.Background.Behaviors
{
    /// <summary>
    /// Standard behavior: travels distance, fades based on lifetime progress.
    /// Configurable fade-in and fade-out points.
    /// </summary>
    [CreateAssetMenu(fileName = "StandardBehavior", menuName = "Starfire/Background/Behaviors/Standard")]
    public class StandardBehaviorConfig : ShootingStarBehaviorConfig
    {
        [Header("Standard Behavior Settings")]
        [Tooltip("Progress at which fade-in completes (0.1 = first 10%)")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float fadeInEnd = 0.1f;

        [Tooltip("Progress at which fade-out begins (0.9 = last 10%)")]
        [Range(0.5f, 0.99f)]
        [SerializeField] private float fadeOutStart = 0.9f;

        public float FadeInEnd => fadeInEnd;
        public float FadeOutStart => fadeOutStart;

        public override ShootingStarBehaviorType BehaviorType => ShootingStarBehaviorType.Standard;

        public override ShootingStarBehaviorData CreateBehaviorData()
        {
            return new ShootingStarBehaviorData
            {
                behaviorType = ShootingStarBehaviorType.Standard,
                fadeParam1 = fadeInEnd,
                fadeParam2 = fadeOutStart,
                fadeParam3 = 0f,
                stateFlags = 0
            };
        }
    }
}
