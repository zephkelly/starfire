namespace Starfire.Entity.Modules.WarpDrive
{
    public interface IWarpDriveModule : IShipModule
    {
        float WarpSpeed { get; }
        float ChargeTime { get; }
        float Cooldown { get; }
        bool IsReady { get; }
        void EngageWarp();
        void DisengageWarp();
    }
}
