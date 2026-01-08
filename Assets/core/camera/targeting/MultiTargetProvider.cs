using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire.Core.Cam.Targeting
{
    public class MultiTargetProvider : ICameraTargetProvider
    {
        private readonly List<ICameraTarget> _targets = new();
        private ICameraTarget _primaryTarget;

        public float Padding { get; set; } = 3f;
        public float MinZoom { get; set; } = 5f;
        public float MaxZoom { get; set; } = 20f;
        public float PrimaryWeight { get; set; } = 2f;

        public bool HasTargets => _targets.Any(t => t.IsValid);

        public Vector2 GetTargetPosition()
        {
            var validTargets = _targets.Where(t => t.IsValid).ToList();
            if (validTargets.Count == 0) return Vector2.zero;

            Vector2 sum = Vector2.zero;
            float totalWeight = 0f;

            foreach (var target in validTargets)
            {
                float weight = target == _primaryTarget ? PrimaryWeight : 1f;
                weight *= target.Priority;
                sum += target.Position * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? sum / totalWeight : Vector2.zero;
        }

        public Vector2 GetInterpolatedPosition()
        {
            var validTargets = _targets.Where(t => t.IsValid).ToList();
            if (validTargets.Count == 0) return Vector2.zero;

            Vector2 sum = Vector2.zero;
            float totalWeight = 0f;

            foreach (var target in validTargets)
            {
                float weight = target == _primaryTarget ? PrimaryWeight : 1f;
                weight *= target.Priority;
                sum += target.InterpolatedPosition * weight;
                totalWeight += weight;
            }

            return totalWeight > 0 ? sum / totalWeight : Vector2.zero;
        }

        public Vector2 GetTargetVelocity()
        {
            var validTargets = _targets.Where(t => t.IsValid).ToList();
            if (validTargets.Count == 0) return Vector2.zero;

            if (_primaryTarget != null && _primaryTarget.IsValid)
            {
                return _primaryTarget.Velocity;
            }

            Vector2 sum = Vector2.zero;
            foreach (var target in validTargets)
            {
                sum += target.Velocity;
            }
            return sum / validTargets.Count;
        }

        public Vector2 GetFocusDirection()
        {
            if (_primaryTarget != null && _primaryTarget.IsValid && _primaryTarget.HasFocus)
            {
                return _primaryTarget.FocusDirection;
            }

            var focusTarget = _targets.FirstOrDefault(t => t.IsValid && t.HasFocus);
            return focusTarget?.FocusDirection ?? Vector2.zero;
        }

        public bool HasFocus
        {
            get
            {
                if (_primaryTarget != null && _primaryTarget.IsValid && _primaryTarget.HasFocus)
                    return true;
                return _targets.Any(t => t.IsValid && t.HasFocus);
            }
        }

        public float GetRecommendedZoom()
        {
            var validTargets = _targets.Where(t => t.IsValid).ToList();
            if (validTargets.Count < 2) return -1f;

            Vector2 center = GetTargetPosition();
            Bounds bounds = new Bounds(new Vector3(center.x, center.y, 0), Vector3.zero);

            foreach (var target in validTargets)
            {
                bounds.Encapsulate(new Vector3(target.Position.x, target.Position.y, 0));
            }

            float requiredHeight = bounds.size.y + Padding * 2;
            float requiredWidth = bounds.size.x + Padding * 2;

            float aspectRatio = Screen.width / (float)Screen.height;
            float zoomFromHeight = requiredHeight / 2f;
            float zoomFromWidth = requiredWidth / (2f * aspectRatio);

            float zoom = Mathf.Max(zoomFromHeight, zoomFromWidth);
            return Mathf.Clamp(zoom, MinZoom, MaxZoom);
        }

        public CameraTargetType GetPrimaryTargetType()
        {
            if (_primaryTarget != null && _primaryTarget.IsValid)
            {
                return _primaryTarget.TargetType;
            }

            var firstValid = _targets.FirstOrDefault(t => t.IsValid);
            return firstValid?.TargetType ?? CameraTargetType.Transform;
        }

        public void AddTarget(ICameraTarget target)
        {
            if (target != null && !_targets.Contains(target))
            {
                _targets.Add(target);
            }
        }

        public void RemoveTarget(ICameraTarget target)
        {
            _targets.Remove(target);
            if (_primaryTarget == target)
            {
                _primaryTarget = _targets.FirstOrDefault(t => t.IsValid);
            }
        }

        public void SetPrimaryTarget(ICameraTarget target)
        {
            _primaryTarget = target;
            if (target != null && !_targets.Contains(target))
            {
                _targets.Add(target);
            }
        }

        public void ClearTargets()
        {
            _targets.Clear();
            _primaryTarget = null;
        }
    }
}
