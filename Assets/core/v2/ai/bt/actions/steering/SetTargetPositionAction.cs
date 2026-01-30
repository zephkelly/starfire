using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Sets a static Vector2 position as the steering target.
    /// Use for moving to fixed points like docking positions or static objectives.
    /// </summary>
    public class SetTargetPositionAction : BTAction
    {
        private readonly Vector2 _position;
        private readonly string _targetKey;

        public SetTargetPositionAction(Vector2 position, string targetKey = "steering_target")
        {
            _position = position;
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            Context.Set(_targetKey, _position);
            return BTNodeStatus.Success;
        }
    }
}
