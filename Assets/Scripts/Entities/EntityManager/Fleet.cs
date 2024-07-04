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
        private List<Ship> ships = new List<Ship>();

        private FleetCommand currentCommand = null;
        private Queue<FleetCommand> commands = new Queue<FleetCommand>();

        [SerializeField] private FleetType fleetType;
        [SerializeField] private FleetAllegience allegience;
        [SerializeField] private Vector2 currentTargetPosition;

        public List<Ship> Ships => ships;
        public Vector2 CurrentTargetPosition => currentTargetPosition;
        public FleetCommand CurrentCommand => currentCommand;

        public Fleet(FleetType _type, FleetAllegience _allegience)
        {
            fleetType = _type;
            allegience = _allegience;
        }

        public void AddShip(Ship ship)
        {
            ships.Add(ship);
        }

        public void AddNewCommand(FleetCommand newCommand, FleetCommand.CommandPriority priority = FleetCommand.CommandPriority.Low)
        {
            if (priority == FleetCommand.CommandPriority.Low)
            {
                commands.Enqueue(newCommand);
            }
        }

        private void SetNewCurrentCommand()
        {
            if (commands.Count > 0)
            {
                currentCommand = commands.Dequeue();

                if (currentCommand.Type == FleetCommand.CommandType.Move)
                {
                    currentTargetPosition = currentCommand.GetTargetPosition();

                    foreach (var ship in ships)
                    {
                        ship.Controller.ShipCore.SetTarget(currentTargetPosition);
                    }
                }
            }
        }

        public void Update()
        {
            if (HasCompletedCurrentCommand())
            {
                currentCommand = null;
                SetNewCurrentCommand();
            } 
        }

        public bool HasCompletedCurrentCommand()
        {
            if (currentCommand == null) return true;

            if (currentCommand.Type == FleetCommand.CommandType.Move)
            {
                bool allShipsReachedTarget = true;

                foreach (var ship in ships)
                {
                    if (Vector2.Distance(ship.Controller.ShipTransform.position, currentTargetPosition) >= 10)
                    {
                        allShipsReachedTarget = false;
                        continue;
                    }
                    else
                    {
                        ship.Controller.ShipCore.RemoveTarget();
                    }
                }

                return allShipsReachedTarget;
            }

            return false;
        }
    }
}