using System.Collections.Generic;
using System.Linq;
using Starfire.Entity.Modules.Sensor;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Selects the closest contact from a list and stores as investigation target.
    /// Returns Success if target selected, Failure if list empty.
    /// </summary>
    public class SelectClosestContactAction : BTAction
    {
        private readonly string _inputKey;
        private readonly string _targetKey;

        public SelectClosestContactAction(
            string inputKey = "sensor_contacts",
            string targetKey = "investigation_target")
        {
            _inputKey = inputKey;
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<List<DetectedEntity>>(_inputKey, out var contacts) || contacts.Count == 0)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] SelectClosestContact: FAIL - No contacts in blackboard");
#endif
                return BTNodeStatus.Failure;
            }

            // List is already sorted by distance from ScanForContactsAction
            var closest = contacts.FirstOrDefault(e => e.IsValid);
            if (!closest.IsValid)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] SelectClosestContact: FAIL - No valid contacts in list of {contacts.Count}");
#endif
                return BTNodeStatus.Failure;
            }

#if UNITY_EDITOR
            if (Context.DebugLogging)
                Debug.Log($"[BT:{Context.EntityName}] SelectClosestContact: SUCCESS - Selected {closest.Controller?.name} at {closest.Distance:F1}m, level {closest.Level}");
#endif
            Context.Set(_targetKey, closest);
            return BTNodeStatus.Success;
        }
    }
}
