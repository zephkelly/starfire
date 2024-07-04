using UnityEngine;

namespace Starfire
{
    public class Ship
    {
        public ShipController Controller { get; private set; }          
        public ShipConfiguration Configuration { get; private set; }    // How the ship is configured
        public Transponder Transponder { get; private set; }            // How in game objects interact
        public Inventory Inventory { get; private set; }

        public Ship(ShipController controller, ShipConfiguration configuration, Transponder transponder, Inventory inventory)
        {
            Controller = controller;
            Configuration = configuration;
            Transponder = transponder;
            Inventory = inventory;
        }
    }
}