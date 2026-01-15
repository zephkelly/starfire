using System;
using Starfire.Entity.AI.BT;
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

        /// <summary>
        /// The centralized behavior tree asset (preferred).
        /// </summary>
        public BehaviorTreeAsset BehaviorTree => behaviorTree;

        /// <summary>
        /// Legacy node-based tree root. Use BehaviorTree instead.
        /// </summary>
        [Obsolete("Use BehaviorTree instead")]
        public BTNodeConfig BehaviorTreeRoot => behaviorTreeRoot;

        public override IAICoreShipModule CreateModule()
        {
            return new BasicAICoreModule(this);
        }
    }
}
