using UnityEngine;

namespace Starfire.Core.Cam.Targeting
{
    public class SingleTargetProvider : ICameraTargetProvider
    {
        private ICameraTarget _target;

        public bool HasTargets => _target != null && _target.IsValid;

        public Vector2 GetTargetPosition()
        {
            return HasTargets ? _target.Position : Vector2.zero;
        }

        public Vector2 GetInterpolatedPosition()
        {
            return HasTargets ? _target.InterpolatedPosition : Vector2.zero;
        }

        public Vector2 GetTargetVelocity()
        {
            return HasTargets ? _target.Velocity : Vector2.zero;
        }

        public Vector2 GetFocusDirection()
        {
            return HasTargets ? _target.FocusDirection : Vector2.zero;
        }

        public bool HasFocus => HasTargets && _target.HasFocus;

        public float GetRecommendedZoom()
        {
            return -1f;
        }

        public CameraTargetType GetPrimaryTargetType()
        {
            return HasTargets ? _target.TargetType : CameraTargetType.Transform;
        }

        public void AddTarget(ICameraTarget target)
        {
            SetPrimaryTarget(target);
        }

        public void RemoveTarget(ICameraTarget target)
        {
            if (_target == target)
            {
                _target = null;
            }
        }

        public void SetPrimaryTarget(ICameraTarget target)
        {
            _target = target;
        }

        public void ClearTargets()
        {
            _target = null;
        }
    }
}
