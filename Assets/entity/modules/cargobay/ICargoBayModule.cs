namespace Starfire.Entity.Modules.CargoBay
{
    public interface ICargoBayModule : IShipModule
    {
        int Capacity { get; }
        int UsedSpace { get; }
        int FreeSpace { get; }
    }
}
