using System;
using System.Collections.Generic;
using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Goals;
using UnityEngine;

namespace Starfire.Entity.Modules.AICore
{
    [CreateAssetMenu(fileName = "BasicAICore", menuName = "Starfire/Modules/AICore/Basic")]
    public class BasicAICoreConfig : AICoreModuleConfig
    {
        [Header("Behavior Tree")]
        [Tooltip("New centralized behavior tree asset (preferred)")]
        [SerializeField] private BehaviorTreeAsset behaviorTree;

        [Tooltip("Legacy node-based tree root (deprecated, use behaviorTree instead)")]
        [Obsolete("Use behaviorTree instead")]
        [SerializeField] private BTNodeConfig behaviorTreeRoot;

        [Header("Goal System")]
        [Tooltip("Default goals available for autonomous selection. The AI will evaluate and choose the best goal.")]
        [SerializeField] private List<GoalParameters> defaultGoals = new();

        [Tooltip("If true, enables the goal system. If false, uses behavior tree without goal management.")]
        [SerializeField] private bool enableGoalSystem = true;

        /// <summary>
        /// The centralized behavior tree asset (preferred).
        /// </summary>
        public BehaviorTreeAsset BehaviorTree => behaviorTree;

        /// <summary>
        /// Legacy node-based tree root. Use BehaviorTree instead.
        /// </summary>
        [Obsolete("Use BehaviorTree instead")]
        public BTNodeConfig BehaviorTreeRoot => behaviorTreeRoot;

        /// <summary>
        /// Default goals available for autonomous selection.
        /// </summary>
        public IReadOnlyList<GoalParameters> DefaultGoals => defaultGoals;

        /// <summary>
        /// If true, enables the goal system for this AI.
        /// </summary>
        public bool EnableGoalSystem => enableGoalSystem;

        public override IAICoreShipModule CreateModule()
        {
            return new BasicAICoreModule(this);
        }
    }
}
