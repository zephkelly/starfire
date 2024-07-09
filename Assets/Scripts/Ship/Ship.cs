namespace Starfire
{
    public class Ship
    {
        public ShipController Controller { get; private set; }
        public ShipConfiguration Configuration { get; private set; } 
        public IAICore AICore { get; private set; }     
        public Transponder Transponder { get; private set; }            // How in game objects communicate
        public Inventory Inventory { get; private set; }

        public Ship(ShipController controller, IAICore aiCore, ShipConfiguration configuration, Transponder transponder, Inventory inventory, Fleet fleet = default)
        {
            Controller = controller;
            AICore = aiCore;
            Configuration = configuration;
            Transponder = transponder;
            Inventory = inventory;

            Controller.SetShip(this);
            AICore.SetShip(this);
        }

        public void InjectAIDependencies(Fleet fleet = default, Blackboard blackboard = default)
        {
            AICore.SetFleet(fleet);
            AICore.SetBlackboard(blackboard);
            AICore.CreateBehaviourTree();
        }
    }
}