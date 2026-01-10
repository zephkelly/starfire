namespace Starfire.Entity.Modules
{
    public interface IEntityModule
    {
        string ModuleId { get; }
        string DisplayName { get; }
        ModuleTier Tier { get; }
        bool IsEnabled { get; set; }

        void OnAttach(EntityControllerBase controller);
        void OnDetach();
        void OnUpdate(float deltaTime);
    }
}
