using System.Collections.Generic;

namespace StarfireV2
{
    /// <summary>
    /// Adds an entity to the identified list so it won't be re-investigated.
    /// Always returns Success.
    /// </summary>
    public class AddToIdentifiedListAction : BTAction
    {
        private readonly string _targetKey;
        private readonly string _listKey;

        public AddToIdentifiedListAction(
            string targetKey = "investigation_target",
            string listKey = "identified_entities")
        {
            _targetKey = targetKey;
            _listKey = listKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            if (!Context.TryGet<V2DetectedEntity>(_targetKey, out var target))
            {
                return BTNodeStatus.Success;
            }

            if (!target.IsValid)
            {
                return BTNodeStatus.Success;
            }

            // Get or create identified list
            if (!Context.TryGet<HashSet<int>>(_listKey, out var identifiedList))
            {
                identifiedList = new HashSet<int>();
                Context.Set(_listKey, identifiedList);
            }

            // Add entity ID to list
            identifiedList.Add(target.Transform.GetInstanceID());

            return BTNodeStatus.Success;
        }
    }
}
