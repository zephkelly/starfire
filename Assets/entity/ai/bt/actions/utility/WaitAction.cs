using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Waits for a specified duration before succeeding.
    /// Returns Running while waiting, Success when complete.
    /// Tracks elapsed time on the blackboard to persist across ticks.
    /// </summary>
    public class WaitAction : BTAction
    {
        private readonly float _duration;
        private readonly string _elapsedKey;

        public WaitAction(float duration = 1.0f, string elapsedKey = "wait_elapsed")
        {
            _duration = duration;
            _elapsedKey = elapsedKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get current elapsed time (or 0 if not set)
            if (!Context.TryGet<float>(_elapsedKey, out var elapsed))
            {
                elapsed = 0f;
            }

            // Accumulate time
            elapsed += deltaTime;
            Context.Set(_elapsedKey, elapsed);

            // Check if we've waited long enough
            if (elapsed >= _duration)
            {
                // Reset for next use
                Context.Set(_elapsedKey, 0f);
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        public override void Reset()
        {
            base.Reset();
            // Also reset elapsed time on tree reset
            Context?.Set(_elapsedKey, 0f);
        }
    }
}
