namespace Starfire.Entity.Modules
{
    public interface IModuleConfig
    {
        ModuleSlotType SlotType { get; }
        IEntityModule CreateModule();
    }
}
