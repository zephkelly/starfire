namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Marker interface for behavior tree nodes designed for Ship entities.
    /// Ship BT nodes have access to BTContext with ShipController and ShipSystems.
    /// </summary>
    public interface IShipBTNode : IBTNode
    {
    }
}
