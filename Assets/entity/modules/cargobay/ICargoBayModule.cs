namespace Starfire.Entity.Modules.CargoBay
{
    public interface ICargoBayModule : IEntityModule
    {
        int Capacity { get; }
        int UsedSpace { get; }
        int FreeSpace { get; }
    }
}
