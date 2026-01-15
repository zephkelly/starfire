namespace Starfire.Entity.Modules
{
    public interface IModuleConfig
    {
        ModuleTypeId TypeId { get; }
        IEntityModule CreateModule();
    }
}
