using UnityEngine;

namespace Starfire.Core
{
    public interface IPlayerController : IController
    {
        bool EscapePressed { get; }
        float ScrollDelta { get; }
        Vector2 MouseScreenPosition { get; }
        Vector3 MouseWorldPosition { get; }
        void SetCamera(Camera camera);
    }
}
