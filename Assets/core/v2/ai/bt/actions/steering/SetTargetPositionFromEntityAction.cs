using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Extracts position from a V2DetectedEntity and writes it as a Vector2.
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
            if (!Context.TryGet<V2DetectedEntity>(_sourceKey, out var entity))
            {
                return BTNodeStatus.Failure;
            }

            if (!entity.IsValid)
            {
                return BTNodeStatus.Failure;
            }

            Vector2 position = entity.Position;
            Context.Set(_outputKey, position);


            return BTNodeStatus.Success;
        }
    }
}
