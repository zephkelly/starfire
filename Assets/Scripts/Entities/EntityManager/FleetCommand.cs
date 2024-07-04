using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public class FleetCommand
    {
        public enum CommandType
        {
            Move,
            Idle,
            Attack
        }

        public enum CommandPriority
        {
            Low,
            High
        }

        public CommandType Type { get; private set; }

        private Vector2 targetPosition;
        private Ship targetShip;

        public FleetCommand(CommandType _type, Vector2 _targetPosition = default)
        {
            Type = _type;
            targetPosition = _targetPosition;
        }

        public FleetCommand(CommandType _type, Ship _targetShip)
        {
            Type = _type;
            targetShip = _targetShip;
        }

        public Vector2 GetTargetPosition()
        {
            return targetPosition;
        }

        public Ship GetTargetShip()
        {
            return targetShip;
        }
    }
}