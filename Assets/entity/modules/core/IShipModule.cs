namespace Starfire.Entity.Modules
{
    public interface IShipModule
    {
        string ModuleId { get; }
        string DisplayName { get; }
        ModuleTier Tier { get; }
        bool IsEnabled { get; set; }

        void OnAttach(EntityController controller);
        void OnDetach();
        void OnUpdate(float deltaTime);
    }
}
