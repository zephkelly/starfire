using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// AI Core module that provides autonomous behavior tree execution for ship entities.
    /// Creates and manages an AIEntityControllerDriver that gets pushed to the controller's driver stack.
    /// </summary>
    public class V2AICoreModule : IAICoreModule
    {
        private readonly AICoreModuleData _data;
        private IEntityController _controller;
        private BTContext _btContext;
        private GoalManager _goalManager;
        private GoalParameters _currentGoalParameters;

        public string ModuleId => _data.moduleId;
        public bool IsEnabled { get; set; } = true;
        public ShipModuleCategory Category => ShipModuleCategory.Utility;
        public ShipModuleType Type => ShipModuleType.AICore;

        public float ProcessingPower => _data.processingPower;
        public bool IsAutonomous { get; set; }

        public AIEntityControllerDriver Driver { get; private set; }
        public IBTNode BehaviorTree { get; private set; }
        public BTContext Context => _btContext;

        public GoalManager GoalManager => _goalManager;
        public GoalParameters CurrentGoalParameters => _currentGoalParameters;

        public V2AICoreModule(AICoreModuleData data)
        {
            _data = data;
        }

        public void OnAttach(IEntityController controller)
        {
            _controller = controller;

            // Reuse existing AI driver from stack (may be serialized in Inspector)
            Driver = controller.DriverStack.Find<AIEntityControllerDriver>();
            if (Driver == null)
            {
                Driver = new AIEntityControllerDriver(priority: 5);
                controller.DriverStack.Push(Driver);
            }

            _btContext = new BTContext(controller, Driver);
            IsAutonomous = true;

            // Initialize goal system if enabled
            if (_data.enableGoalSystem)
            {
                _goalManager = new GoalManager();
                _goalManager.Initialize(_btContext, _data.defaultGoals);
            }

            // Create behavior tree from data
            if (BehaviorTree == null && _data.behaviorTree != null)
            {
                BehaviorTree = _data.behaviorTree.CreateRuntimeTree();
            }

            BehaviorTree?.Initialize(_btContext);
        }

        public void ApplyGoalParameters(GoalParameters parameters, bool clearPrevious = true)
        {
            if (_btContext == null) return;

            if (clearPrevious && _currentGoalParameters != null)
            {
                _currentGoalParameters.ClearFromBlackboard(_btContext);
            }

            _currentGoalParameters = parameters;
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
