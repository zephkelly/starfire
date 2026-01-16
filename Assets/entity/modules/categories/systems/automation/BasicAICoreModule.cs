using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Goals;
using UnityEngine;

namespace Starfire.Entity.Modules.AICore
{
    public class BasicAICoreModule : IAICoreShipModule
    {
        private bool _hasLoggedOnce = false;
        private readonly BasicAICoreConfig _config;
        private EntityControllerBase _controller;
        private BTContext _btContext;
        private GoalManager _goalManager;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public float ProcessingPower => _config.ProcessingPower;
        public bool IsAutonomous { get; set; }

        public AIDriver Driver { get; private set; }
        public IBTNode BehaviorTree { get; private set; }
        public BTContext Context => _btContext;

        public BasicAICoreConfig Config => _config;
        public GoalParameters CurrentGoalParameters { get; private set; }

        /// <summary>
        /// The goal manager for this AI. May be null if goal system is disabled.
        /// </summary>
        public GoalManager GoalManager => _goalManager;

        public BasicAICoreModule(BasicAICoreConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;

            Driver = new AIDriver(priority: 5);
            controller.DriverStack.Push(Driver);

            _btContext = new BTContext(controller, Driver);

            // Initialize goal system if enabled
            if (_config.EnableGoalSystem)
            {
                _goalManager = new GoalManager();
                _goalManager.Initialize(_btContext, _config.DefaultGoals);
            }

            // Auto-create behavior tree from config if not already set
            if (BehaviorTree == null)
            {
                // Try new centralized BehaviorTreeAsset first (preferred)
                if (_config.BehaviorTree != null)
                {
                    BehaviorTree = _config.BehaviorTree.CreateRuntimeTree();
                }
                // Fall back to legacy BTNodeConfig for backwards compatibility
#pragma warning disable CS0618 // Suppress obsolete warning
                else if (_config.BehaviorTreeRoot != null)
                {
                    BehaviorTree = _config.BehaviorTreeRoot.CreateNode();
                }
#pragma warning restore CS0618
            }

            BehaviorTree?.Initialize(_btContext);
        }

        /// <summary>
        /// Applies goal parameters to the blackboard, enabling dynamic behavior configuration.
        /// </summary>
        /// <param name="parameters">The goal parameters to apply.</param>
        /// <param name="clearPrevious">If true, clears the previous goal parameters first.</param>
        public void ApplyGoalParameters(GoalParameters parameters, bool clearPrevious = true)
        {
            if (_btContext == null) return;

            if (clearPrevious && CurrentGoalParameters != null)
            {
                CurrentGoalParameters.ClearFromBlackboard(_btContext);
            }

            CurrentGoalParameters = parameters;
            parameters?.WriteToBlackboard(_btContext);
        }

        public void OnDetach()
        {
            _controller?.DriverStack.Remove(Driver);
            Driver = null;
            _btContext = null;
            _controller = null;
        }

        public void OnUpdate(float deltaTime)
        {
            if (!_hasLoggedOnce)
            {
                _hasLoggedOnce = true;
            }

            if (!IsEnabled || !IsAutonomous)
            {
                return;
            }

            BehaviorTree?.Execute(deltaTime);
        }

        public void SetBehaviorTree(IBTNode tree)
        {
            BehaviorTree = tree;
            if (_btContext != null)
            {
                tree?.Initialize(_btContext);
            }
        }
    }
}
