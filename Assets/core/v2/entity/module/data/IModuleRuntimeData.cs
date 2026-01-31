namespace StarfireV2
{
    public interface IModuleRuntimeData
    {
        string ModuleId { get; }
        ShipModuleTypeId TypeId { get; }
    }
}
