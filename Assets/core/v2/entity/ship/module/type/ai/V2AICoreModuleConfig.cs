using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration asset for V2 AI Core modules.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAICoreConfig", menuName = "StarfireV2/Modules/AI Core")]
    public class V2AICoreModuleConfig : ScriptableObject, IShipModuleConfig
    {
        [Header("Module Identity")]
        [SerializeField] private string moduleId = "ai_core";

        [Header("AI Configuration")]
        [SerializeField] private float processingPower = 1f;

        [Tooltip("The behavior tree asset to use for this AI.")]
        [SerializeField] private BehaviorTreeAsset behaviorTree;

        [Header("Goal System")]
        [SerializeField] private bool enableGoalSystem = true;

        [Tooltip("Default goal parameter assets to load on initialization.")]
        [SerializeField] private List<GoalParameters> defaultGoals = new();

        public string ModuleId => moduleId;
        public float ProcessingPower => processingPower;
        public BehaviorTreeAsset BehaviorTree => behaviorTree;
        public bool EnableGoalSystem => enableGoalSystem;
        public IReadOnlyList<GoalParameters> DefaultGoals => defaultGoals;

        public ShipModuleTypeId TypeId => ShipModuleTypeId.AICore;

        public IModuleRuntimeData ToData()
        {
            return new AICoreModuleData
            {
                moduleId = moduleId,
                processingPower = processingPower,
                behaviorTree = behaviorTree,
                enableGoalSystem = enableGoalSystem,
                defaultGoals = new System.Collections.Generic.List<GoalParameters>(defaultGoals)
            };
        }

        public IShipModule CreateModuleFromData(IModuleRuntimeData data)
        {
            return new V2AICoreModule((AICoreModuleData)data);
        }

        public IShipModule CreateModule()
        {
            return new V2AICoreModule((AICoreModuleData)ToData());
        }
    }
}
