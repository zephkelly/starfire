namespace Starfire.Entity.Modules.LifeSupport
{
    public interface ILifeSupportShipModule : IShipModule
    {
        int CrewCapacity { get; }
    }
}
