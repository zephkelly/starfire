using Starfire.Entity.AI.BT;
using UnityEngine;

namespace Starfire.Entity.Modules.AICore
{
    public class BasicAICoreModule : IAICoreModule
    {
        private bool _hasLoggedOnce = false;
        private readonly BasicAICoreConfig _config;
        private EntityControllerBase _controller;
        private BTContext _btContext;

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
                Debug.Log($"[BasicAICoreModule] OnUpdate check: IsEnabled={IsEnabled}, IsAutonomous={IsAutonomous}, BehaviorTree={(BehaviorTree != null ? "exists" : "NULL")}");
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
