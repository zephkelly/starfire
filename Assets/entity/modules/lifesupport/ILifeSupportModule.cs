namespace Starfire.Entity.Modules.LifeSupport
{
    public interface ILifeSupportModule : IShipModule
    {
        int CrewCapacity { get; }
    }
}
