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

        [SerializeField] private Ship flagship;
        private List<Ship> ships = new List<Ship>();

        [SerializeField] private FleetType fleetType;
        [SerializeField] private FleetAllegience allegience;

        public Ship Flagship => flagship;
        public List<Ship> Ships => ships;

        public Fleet(FleetType _type, FleetAllegience _allegience)
        {
            fleetType = _type;
            allegience = _allegience;

            fleetStateMachine = new StateMachine();
            fleetStateMachine.ChangeState(new FleetIdleState(this));
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