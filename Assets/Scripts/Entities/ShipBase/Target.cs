using UnityEngine;

namespace Starfire
{
    public enum TargetType
    {
        Ship,
        Position
    }

    public class Target
    {
        public TargetType Type { get; private set; }

        private Vector2 targetPosition;
        private Transform targetTransform;
        private Rigidbody2D targetRigidbody2D;

        public Target(Transform _targetTransform, Rigidbody2D _targetRigidbody2D)
        {
            Type = TargetType.Ship;
            targetTransform = _targetTransform;
            targetRigidbody2D = _targetRigidbody2D;
        }

        public Target(Vector2 position)
        {
            Type = TargetType.Position;
            targetPosition = position;

            targetTransform = null;
            targetRigidbody2D = null;
        }

        public bool IsDestroyed()
        {
            if (Type == TargetType.Ship)
            {
                if (targetTransform == null || targetRigidbody2D == null)
                {
                    return true;
                }
            }
            else if (Type == TargetType.Position)
            {
                if (targetPosition == Vector2.zero)
                {
                    return true;
                }
            }

            return false;
        }

        public Vector2 GetPosition()
        {
            if (Type == TargetType.Position) return targetPosition;

            // if (targetTransform == null) return Vector2.zero;
            return targetTransform.position;
        }

        public Vector2 GetVelocity()
        {
            // if (targetRigidbody2D == null) return Vector2.zero;
            return targetRigidbody2D.velocity;
        }

        public Vector2 GetVelocityMagnitude()
        {
            // if (targetRigidbody2D == null) return Vector2.zero;
            return targetRigidbody2D.velocity.normalized;
        }

        public bool IsSameTargetAs(Vector2 newTargetPosition)
        {
            if (Type is not TargetType.Position)
            {
                Debug.LogWarning("Target.IsSameTargetAs: Target is not a position");
                return false;
            }

            return targetPosition == newTargetPosition;
        }
        public bool IsSameTargetAs(Transform newTargetTransform)
        {
            if (Type is not TargetType.Ship)
            {
                Debug.LogWarning("Target.IsSameTargetAs: Target is not a ship");
                return false;
            }
            return targetTransform == newTargetTransform;
        }
    }
}