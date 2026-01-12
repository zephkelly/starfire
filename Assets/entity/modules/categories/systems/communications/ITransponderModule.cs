namespace Starfire.Entity.Modules.Transponder
{
    public interface ITransponderModule : IEntityModule
    {
        string ShipId { get; }
        string Faction { get; }
        bool IsTransmitting { get; set; }
    }
}
