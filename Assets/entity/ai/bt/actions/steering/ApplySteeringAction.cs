using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Applies a steering force from the blackboard to the AIDriver.
    /// The force is then applied to the rigidbody by the controller.
    /// </summary>
    public class ApplySteeringAction : BTAction
    {
        private readonly string _forceKey;

        public ApplySteeringAction(string forceKey = "steering_force")
        {
            _forceKey = forceKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get steering force from blackboard
            if (!Context.TryGet<Vector2>(_forceKey, out var force))
            {
                Debug.LogWarning($"[ApplySteering] FAILURE: No force found in blackboard key '{_forceKey}'");
                return BTNodeStatus.Failure;
            }

            // Apply via AIDriver's DesiredAcceleration
            Context.Driver.DesiredAcceleration = force;

            return BTNodeStatus.Success;
        }
    }
}
