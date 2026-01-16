using UnityEngine;
using Starfire.Entity.Modules.Sensor;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Extracts position from a DetectedEntity and writes it as a Vector2.
    /// Used to set rotation target separately from steering target.
    /// </summary>
    public class SetTargetPositionFromEntityAction : BTAction
    {
        private readonly string _sourceKey;
        private readonly string _outputKey;

        public SetTargetPositionFromEntityAction(
            string sourceKey = "monitored_target",
            string outputKey = "rotation_target")
        {
            _sourceKey = sourceKey;
            _outputKey = outputKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<DetectedEntity>(_sourceKey, out var entity))
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] SetTargetPositionFromEntity: FAIL - No entity at '{_sourceKey}'");
#endif
                return BTNodeStatus.Failure;
            }

            if (!entity.IsValid)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] SetTargetPositionFromEntity: FAIL - Entity is not valid");
#endif
                return BTNodeStatus.Failure;
            }

            Vector2 position = entity.Controller.transform.position;
            Context.Set(_outputKey, position);

#if UNITY_EDITOR
            if (Context.DebugLogging)
                Debug.Log($"[BT:{Context.EntityName}] SetTargetPositionFromEntity: Set '{_outputKey}' to {position}");
#endif

            return BTNodeStatus.Success;
        }
    }
}
