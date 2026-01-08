using UnityEngine;

namespace Starfire.Entity.Modules.Rotation
{
    public struct RotationInputData
    {
        public float KeyboardInput;
        public Vector2 MouseWorldPosition;
        public Vector2 EntityPosition;
        public float CurrentRotation;

        public float GetTargetAngle(float spriteOffset = -90f)
        {
            Vector2 direction = MouseWorldPosition - EntityPosition;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteOffset;
        }

        public float GetAngleDelta(float spriteOffset = -90f)
        {
            float targetAngle = GetTargetAngle(spriteOffset);
            return Mathf.DeltaAngle(CurrentRotation, targetAngle);
        }
    }
}
