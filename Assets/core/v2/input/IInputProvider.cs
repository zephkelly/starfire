using System;
using UnityEngine;

namespace StarfireV2
{
    public interface IInputProvider
    {
        event Action<Vector2> OnMove;
        event Action<float> OnRotate;
        event Action<Vector2> OnAim;

        event Action<bool> OnFire;
        event Action OnWarpPressed;
        event Action OnWarpReleased;
        event Action OnHyperdrivePressed;
        event Action OnHyperdriveReleased;
    }
}