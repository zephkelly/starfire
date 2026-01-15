namespace Starfire.Entity.Modules.CargoBay
{
    public interface ICargoBayShipModule : IShipModule
    {
        int Capacity { get; }
        int UsedSpace { get; }
        int FreeSpace { get; }
    }
}
