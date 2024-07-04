using UnityEngine;

namespace Starfire
{
    public class Ship
    {
        public ShipConfiguration Configuration { get; private set; }    // How the ship is configured
        public Transponder Transponder { get; private set; }            // How in game objects interact
        public Inventory Inventory { get; private set; }

        public Ship(ShipConfiguration configuration, Transponder transponder, Inventory inventory)
        {
            Configuration = configuration;
            Transponder = transponder;
            Inventory = inventory;
        }
    }
}