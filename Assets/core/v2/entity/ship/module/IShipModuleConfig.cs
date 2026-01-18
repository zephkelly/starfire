namespace StarfireV2 
{
    public interface IShipModuleConfig
    {
        ShipModuleTypeId TypeId { get; }
        IShipModule CreateModule();
    }
}