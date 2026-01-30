namespace StarfireV2
{
    /// <summary>
    /// Marker interface for behavior tree nodes designed for Ship entities.
    /// Ship BT nodes have access to BTContext with IEntityController and ShipModuleSlotCollection.
    /// </summary>
    public interface IShipBTNode : IBTNode
    {
    }
}
