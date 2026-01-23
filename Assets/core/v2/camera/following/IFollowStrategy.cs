using Starfire.Core.V2.Cam.Config;
using UnityEngine;

namespace Starfire.Core.V2.Cam.Following
{
    public interface IFollowStrategy
    {
        Vector2 CurrentPosition { get; }
        Vector2 CurrentVelocity { get; }
        float TrackingFactor { get; }
        Vector2 PositionError { get; }
        Vector2 FocusOffset { get; }

        void Initialize(Vector2 startPosition);
        Vector2 Update(Vector2 targetPosition, Vector2 targetVelocity, Vector2 focusDirection, float currentZoom, float deltaTime);
        void SnapTo(Vector2 position);
        void SetPreset(VelocityCameraPresetInstance preset);
        void ShiftPosition(Vector2 shiftAmount);
    }
}
