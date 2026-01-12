using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds based on a random probability.
    /// Can optionally cache the result until a trigger value changes
    /// (e.g., re-roll when waypoint index changes).
    /// </summary>
    public class RandomChanceCondition : BTAction
    {
        private readonly float _chance;
        private readonly bool _evaluateOnce;
        private readonly string _resultKey;
        private readonly string _resetTriggerKey;
        private readonly string _lastTriggerValueKey;

        public RandomChanceCondition(
            float chance = 0.5f,
            bool evaluateOnce = false,
            string resultKey = "random_chance_result",
            string resetTriggerKey = "waypoint_index")
        {
            _chance = Mathf.Clamp01(chance);
            _evaluateOnce = evaluateOnce;
            _resultKey = resultKey;
            _resetTriggerKey = resetTriggerKey;
            _lastTriggerValueKey = resultKey + "_last_trigger";
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            bool result;

            if (_evaluateOnce)
            {
                // Check if trigger value changed (e.g., waypoint index)
                bool shouldReroll = false;

                if (Context.TryGet<int>(_resetTriggerKey, out var currentTrigger))
                {
                    if (Context.TryGet<int>(_lastTriggerValueKey, out var lastTrigger))
                    {
                        shouldReroll = currentTrigger != lastTrigger;
                    }
                    else
                    {
                        // First time seeing the trigger
                        shouldReroll = true;
                    }
                    Context.Set(_lastTriggerValueKey, currentTrigger);
                }

                // Check for cached result
                if (!shouldReroll && Context.TryGet<bool>(_resultKey, out var cachedResult))
                {
                    result = cachedResult;
                }
                else
                {
                    // Roll new result
                    result = Random.value <= _chance;
                    Context.Set(_resultKey, result);
                }
            }
            else
            {
                // Always re-roll
                result = Random.value <= _chance;
            }

            return result ? BTNodeStatus.Success : BTNodeStatus.Failure;
        }
    }
}
