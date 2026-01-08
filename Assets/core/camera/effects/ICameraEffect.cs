using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    public interface ICameraEffect
    {
        string EffectId { get; }
        bool IsActive { get; }

        void Initialize(CameraController controller);
        void Update(float deltaTime);
        void Apply(ref Vector3 position, ref float rotation, ref float zoom);
        void Reset();
    }
}
