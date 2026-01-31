using System;
using System.Collections.Generic;

namespace StarfireV2
{
    [Serializable]
    public class AICoreModuleData : IModuleRuntimeData
    {
        public string moduleId = "ai_core";
        public float processingPower = 1f;
        public BehaviorTreeAsset behaviorTree;
        public bool enableGoalSystem = true;
        public List<GoalParameters> defaultGoals = new();

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.AICore;
    }
}
