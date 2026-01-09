namespace Starfire.Entity.Modules.Transponder
{
    public interface ITransponderModule : IShipModule
    {
        string ShipId { get; }
        string Faction { get; }
        bool IsTransmitting { get; set; }
    }
}
