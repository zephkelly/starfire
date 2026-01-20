using UnityEngine;

namespace StarfireV2
{
    public interface IShipOffensiveModule : IShipModule
    {
        float Damage { get; }
        float FireRate { get; }
        float Range { get; }
        bool CanFire { get; }

        bool Fire();
        
        void SetAimDirection(Vector2 worldDirection);
        void OnHardpointAssigned();
    }
}