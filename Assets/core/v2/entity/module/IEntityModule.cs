namespace StarfireV2
{
    public interface IEntityModule
    {
        string ModuleId { get; }
        bool IsEnabled { get; set; }

        void OnAttach(IEntityController controller);
        void OnDetach();
        void OnUpdate(float deltaTime);
    }
}
