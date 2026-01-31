using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Coordinates target assignment across multiple point defense modules on the same ship.
    /// Distributes targets so that each PD engages a different threat when possible,
    /// preventing all turrets from focusing on a single target while others go unengaged.
    /// </summary>
    public class PointDefenseCoordinator
    {
        private static readonly Dictionary<int, PointDefenseCoordinator> _coordinators = new();

        private readonly List<PointDefenseModule> _modules = new();
        private readonly Dictionary<PointDefenseModule, Transform> _assignments = new();

        /// <summary>
        /// Gets or creates a coordinator for the given controller.
        /// </summary>
        public static PointDefenseCoordinator GetOrCreate(IEntityController controller)
        {
            int id = controller.Transform.GetInstanceID();
            if (!_coordinators.TryGetValue(id, out var coordinator))
            {
                coordinator = new PointDefenseCoordinator();
                _coordinators[id] = coordinator;
            }
            return coordinator;
        }

        /// <summary>
        /// Removes the coordinator for the given controller if it has no remaining modules.
        /// </summary>
        public static void TryRemove(IEntityController controller)
        {
            if (controller?.Transform == null) return;
            int id = controller.Transform.GetInstanceID();
            if (_coordinators.TryGetValue(id, out var coordinator) && coordinator._modules.Count == 0)
            {
                _coordinators.Remove(id);
            }
        }

        public void Register(PointDefenseModule module)
        {
            if (!_modules.Contains(module))
                _modules.Add(module);
        }

        public void Unregister(PointDefenseModule module)
        {
            _modules.Remove(module);
            _assignments.Remove(module);
        }

        /// <summary>
        /// Reports the target this module is currently engaging, so other modules can avoid it.
        /// </summary>
        public void ReportEngagement(PointDefenseModule module, Transform target)
        {
            if (target != null)
                _assignments[module] = target;
            else
                _assignments.Remove(module);
        }

        /// <summary>
        /// Returns true if another module is already engaging this target.
        /// </summary>
        public bool IsTargetEngagedByOther(PointDefenseModule requester, Transform target)
        {
            foreach (var kvp in _assignments)
            {
                if (kvp.Key != requester && kvp.Value == target)
                    return true;
            }
            return false;
        }
    }
}
