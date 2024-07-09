using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public enum FleetType
    {
        Military,
        Civilian
    }

    public enum FleetAllegience
    {
        Friend,
        Enemy,
        Neutral
    }

    [System.Serializable]
    public class Fleet
    {
        private StateMachine fleetStateMachine;
        private FleetBlackboard fleetBlackboard;

        [SerializeField] private Ship flagship;
        private List<Ship> ships = new List<Ship>();

        [SerializeField] private FleetType fleetType;
        [SerializeField] private FleetAllegience fleetAllegience;

        public FleetBlackboard FleetBlackboard => fleetBlackboard;
        public Ship Flagship => flagship;
        public IReadOnlyList<Ship> Ships => ships.AsReadOnly();

        public Fleet(FleetType fleetType, FleetAllegience fleetAllegience)
        {
            this.fleetType = fleetType;
            this.fleetAllegience = fleetAllegience;

            fleetStateMachine = new StateMachine();
            fleetStateMachine.ChangeState(new FleetIdleState(this));

            fleetBlackboard = new FleetBlackboard();
        }

        public void AddShip(Ship ship)
        {
            ships.Add(ship);
        }

        public void SetFlagship(Ship ship)
        {
            flagship = ship;
        }

        public void Update()
        {
            fleetStateMachine.Update();
        }
    }
}