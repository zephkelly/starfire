namespace Starfire.Entity.Modules.CargoBay
{
    public class BasicCargoBayModule : ICargoBayModule
    {
        private readonly BasicCargoBayConfig _config;
        private EntityControllerBase _controller;

        public string ModuleId => _config.ModuleId;
        public string DisplayName => _config.DisplayName;
        public ModuleTier Tier => _config.Tier;
        public bool IsEnabled { get; set; } = true;

        public int Capacity => _config.Capacity;
        public int UsedSpace { get; private set; }
        public int FreeSpace => Capacity - UsedSpace;

        public BasicCargoBayModule(BasicCargoBayConfig config)
        {
            _config = config;
        }

        public void OnAttach(EntityControllerBase controller)
        {
            _controller = controller;
        }

        public void OnDetach()
        {
            _controller = null;
        }

        public void OnUpdate(float deltaTime) { }
    }
}
