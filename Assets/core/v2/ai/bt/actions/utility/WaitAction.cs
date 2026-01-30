using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Waits for a specified duration before succeeding.
    /// Returns Running while waiting, Success when complete.
    /// Tracks elapsed time on the blackboard to persist across ticks.
    /// Supports random variance via randomDelay parameter.
    /// </summary>
    public class WaitAction : BTAction
    {
        private readonly float _duration;
        private readonly float _randomDelay;
        private readonly string _elapsedKey;
        private readonly string _targetKey;

        public WaitAction(float duration = 1.0f, float randomDelay = 0f, string elapsedKey = "wait_elapsed")
        {
            _duration = duration;
            _randomDelay = randomDelay;
            _elapsedKey = elapsedKey;
            _targetKey = elapsedKey + "_target";
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // First tick: calculate randomized target duration
            if (!Context.TryGet<float>(_targetKey, out var targetDuration))
            {
                targetDuration = _duration + Random.Range(0f, _randomDelay);
                Context.Set(_targetKey, targetDuration);
            }

            // Get current elapsed time (or 0 if not set)
            if (!Context.TryGet<float>(_elapsedKey, out var elapsed))
            {
                elapsed = 0f;
            }

            // Accumulate time
            elapsed += deltaTime;
            Context.Set(_elapsedKey, elapsed);

            // Check if we've waited long enough
            if (elapsed >= targetDuration)
            {
                // Reset for next use
                Context.Set(_elapsedKey, 0f);
                Context.Set(_targetKey, 0f);
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        public override void Reset()
        {
            base.Reset();
            // Reset elapsed time and target on tree reset
            Context?.Set(_elapsedKey, 0f);
            Context?.Set(_targetKey, 0f);
        }
    }
}
