using UnityEngine;

namespace Starfire.Core.Background.Behaviors
{
    /// <summary>
    /// Simple time-based fade behavior configuration.
    /// Stars fade from full brightness to zero over a specified duration.
    /// </summary>
    [CreateAssetMenu(fileName = "SimpleTimeFadeBehavior", menuName = "Starfire/Background/Behaviors/SimpleTimeFade")]
    public class SimpleTimeFadeBehaviorConfig : ShootingStarBehaviorConfig
    {
        [Header("Fade Settings")]
        [Tooltip("Total time in seconds to fade from full brightness to zero")]
        [Min(0.1f)]
        [SerializeField] private float fadeDuration = 3f;

        [Tooltip("Delay in seconds before fade begins (star stays at full brightness)")]
        [Min(0f)]
        [SerializeField] private float fadeDelay = 0f;

        public override ShootingStarBehaviorType BehaviorType => ShootingStarBehaviorType.SimpleTimeFade;

        public override ShootingStarBehaviorData CreateBehaviorData()
        {
            return new ShootingStarBehaviorData
            {
                behaviorType = ShootingStarBehaviorType.SimpleTimeFade,
                fadeParam1 = fadeDuration,
                fadeParam2 = fadeDelay,
                fadeParam3 = 0f,
                stateFlags = 0
            };
        }
    }
}
