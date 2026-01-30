namespace StarfireV2
{
    public interface IBTNode
    {
        void Initialize(BTContext context);
        BTNodeStatus Execute(float deltaTime);
        void Reset();
    }
}
