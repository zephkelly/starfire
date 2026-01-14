namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Defines how the minimap orients relative to the player.
    /// </summary>
    public enum MinimapOrientationMode
    {
        /// <summary>
        /// Minimap stays fixed (north/positive Y is up), player icon rotates to show facing direction.
        /// </summary>
        NorthUp,

        /// <summary>
        /// Player ship always points up, minimap rotates around player.
        /// </summary>
        ShipUp
    }
}
