using UnityEngine;

namespace Starfire.Core.Cam.Targeting
{
    public interface ICameraTargetProvider
    {
        Vector2 GetTargetPosition();
        Vector2 GetInterpolatedPosition();
        Vector2 GetTargetVelocity();
        Vector2 GetFocusDirection();
        bool HasFocus { get; }
        float GetRecommendedZoom();
        bool HasTargets { get; }
        CameraTargetType GetPrimaryTargetType();
        void AddTarget(ICameraTarget target);
        void RemoveTarget(ICameraTarget target);
        void SetPrimaryTarget(ICameraTarget target);
        void ClearTargets();
    }
}
