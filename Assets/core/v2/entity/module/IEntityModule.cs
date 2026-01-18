namespace StarfireV2
{
    public interface IEntityModule
    {
        string Id { get; }
        bool IsEnabled { get; set; }

        void OnAttach(IEntityController controller);
        void OnDetach();
        void OnUpdate(float deltaTime);
    }
}
