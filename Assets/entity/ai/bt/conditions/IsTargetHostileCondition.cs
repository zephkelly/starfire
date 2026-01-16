using System.Linq;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when investigation target is hostile faction.
    /// Requires target to be at Silhouette or better detection level.
    /// </summary>
    public class IsTargetHostileCondition : BTLeafCondition
    {
        private readonly string _targetKey;

        public IsTargetHostileCondition(string targetKey = "investigation_target")
        {
            _targetKey = targetKey;
        }

        protected override bool CheckCondition()
        {
            if (!Context.TryGet<DetectedEntity>(_targetKey, out var target))
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] IsTargetHostile: FAIL - No target in blackboard");
#endif
                return false;
            }

            if (!target.IsValid)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] IsTargetHostile: FAIL - Target invalid (controller null)");
#endif
                return false;
            }

            // Get target faction (only available at Silhouette or better)
            var targetFaction = target.Faction;
            if (targetFaction == null)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] IsTargetHostile: FAIL - Target faction null (detection level: {target.Level}, need Silhouette+)");
#endif
                return false;
            }

            // Get own faction from transponder
            var ownTransponder = Context.Systems?.GetAllModulesOfType<ITransponderShipModule>().FirstOrDefault();
            if (ownTransponder?.Faction == null)
            {
#if UNITY_EDITOR
                if (Context.DebugLogging)
                    Debug.Log($"[BT:{Context.EntityName}] IsTargetHostile: FAIL - Own transponder/faction null");
#endif
                return false;
            }

            bool isHostile = ownTransponder.Faction.IsHostileTo(targetFaction);
#if UNITY_EDITOR
            if (Context.DebugLogging)
                Debug.Log($"[BT:{Context.EntityName}] IsTargetHostile: {(isHostile ? "SUCCESS" : "FAIL")} - Own:{ownTransponder.Faction.name} vs Target:{targetFaction.name} = {isHostile}");
#endif
            return isHostile;
        }
    }
}
