namespace StarfireV2
{
    public interface IShipModule : IEntityModule
    {
        ShipModuleCategory Category { get; }
        ShipModuleType Type { get; }
    }
}