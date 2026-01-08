using UnityEngine;

namespace Starfire.Core.Background.Behaviors
{
    /// <summary>
    /// Slow fade behavior: fades out slowly based on time OR distance.
    /// </summary>
    [CreateAssetMenu(fileName = "SlowFadeBehavior", menuName = "Starfire/Background/Behaviors/SlowFade")]
    public class SlowFadeBehaviorConfig : ShootingStarBehaviorConfig
    {
        [Header("Slow Fade Settings")]
        [SerializeField] private SlowFadeMode fadeMode = SlowFadeMode.TimeBased;

        [Tooltip("For TimeBased: total fade duration in seconds")]
        [Min(0.1f)]
        [SerializeField] private float fadeDuration = 5f;

        [Tooltip("For DistanceBased: distance traveled before fully faded")]
        [Min(1f)]
        [SerializeField] private float fadeDistance = 30f;

        [Tooltip("Delay before fade begins (seconds for TimeBased, distance units for DistanceBased)")]
        [Min(0f)]
        [SerializeField] private float fadeDelay = 1f;

        public SlowFadeMode FadeMode => fadeMode;
        public float FadeDuration => fadeDuration;
        public float FadeDistance => fadeDistance;
        public float FadeDelay => fadeDelay;

        public override ShootingStarBehaviorType BehaviorType => ShootingStarBehaviorType.SlowFade;

        public override ShootingStarBehaviorData CreateBehaviorData()
        {
            return new ShootingStarBehaviorData
            {
                behaviorType = ShootingStarBehaviorType.SlowFade,
                fadeParam1 = fadeMode == SlowFadeMode.TimeBased ? fadeDuration : fadeDistance,
                fadeParam2 = fadeDelay,
                fadeParam3 = (int)fadeMode,
                stateFlags = 0
            };
        }
    }
}
