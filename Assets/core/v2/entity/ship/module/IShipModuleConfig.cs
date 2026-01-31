namespace StarfireV2
{
    public interface IShipModuleConfig
    {
        ShipModuleTypeId TypeId { get; }
        IShipModule CreateModule();
        IModuleRuntimeData ToData();
        IShipModule CreateModuleFromData(IModuleRuntimeData data);
    }
}