using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Copies a detected entity from one blackboard key to another for monitoring.
    /// Used to transition from investigation to monitoring mode.
    /// Returns Success if target copied, Failure if source not found.
    /// </summary>
    public class StoreMonitoredTargetAction : BTAction
    {
        private readonly string _sourceKey;
        private readonly string _targetKey;

        public StoreMonitoredTargetAction(
            string sourceKey = "investigation_target",
            string targetKey = "monitored_target")
        {
            _sourceKey = sourceKey;
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<V2DetectedEntity>(_sourceKey, out var entity))
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] StoreMonitoredTarget: FAIL - No entity at '{_sourceKey}'");
#endif
                return BTNodeStatus.Failure;
            }

            if (!entity.IsValid)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] StoreMonitoredTarget: FAIL - Entity at '{_sourceKey}' is not valid");
#endif
                return BTNodeStatus.Failure;
            }

            Context.Set(_targetKey, entity);

#if UNITY_EDITOR
            if (Context.DebugLogging)
                Debug.Log($"[BT:{Context.EntityName}] StoreMonitoredTarget: SUCCESS - Stored {entity.Transform?.name} for monitoring");
#endif

            return BTNodeStatus.Success;
        }
    }
}
