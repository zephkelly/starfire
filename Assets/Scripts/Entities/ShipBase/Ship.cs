namespace Starfire
{
    public class Ship
    {
        public ShipController Controller { get; private set; }
        public AICore AICore { get; private set; }     
        public ShipConfiguration Configuration { get; private set; }    // How the ship is configured
        public Transponder Transponder { get; private set; }            // How in game objects communicate
        public Inventory Inventory { get; private set; }

        public Ship(ShipController controller, AICore aiCore, ShipConfiguration configuration, Transponder transponder, Inventory inventory)
        {
            Controller = controller;
            AICore = aiCore;
            Configuration = configuration;
            Transponder = transponder;
            Inventory = inventory;

            InjectDependencies();
        }

        private void InjectDependencies()
        {
            Controller.SetShip(this, AICore);
            AICore.SetShip(this);
        }
    }
}