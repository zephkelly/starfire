namespace Starfire.Entity
{
    public interface IShipAbility
    {
        int AbilityId { get; }
        string DisplayName { get; }
        float Cooldown { get; }
        bool IsActive { get; }

        bool CanActivate();
        void Activate();

        void Update(float deltaTime);
        void Deactivate();

        byte[] CreateStateSnapshot();
        void RestoreStateSnapshot(byte[] snapshot);      
    }
}