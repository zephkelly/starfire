using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

namespace Starfire
{
    public class Command
    {
        public enum TargetType
        {
            Ship,
            Position
        }

        public enum Type
        {
            Move,
            Idle,
            Attack
        }

        public enum Priority
        {
            Low,
            High
        }

        public Type CommandType { get; private set; }
        public TargetType CommandTargetType { get; private set; }
        public Priority CommandPriority { get; private set; }

        private Ship targetShip;
        private Vector2 targetPosition;

        public Command(Type _type, Priority _priority, Vector2 _targetPosition)
        {
            CommandType = _type;
            CommandTargetType = TargetType.Position;
            CommandPriority = _priority;
            targetPosition = _targetPosition;
        }

        public Command(Type _type, Priority _priority, Ship _targetShip)
        {
            CommandType = _type;
            CommandTargetType = TargetType.Ship;
            CommandPriority = _priority;
            targetShip = _targetShip;
        }

        public Vector2 GetTargetPosition()
        {
            if (CommandTargetType == TargetType.Position)
            {
                return targetPosition;
            }

            if (CommandTargetType == TargetType.Ship)
            {
                return targetShip.Controller.ShipTransform.position;
            }

            Debug.LogWarning("Command target type not set.");
            return Vector2.zero;
        }

        public Ship GetTargetShip()
        {
            return targetShip;
        }
    }
}