using System;
using UnityEngine;

namespace StarfireV2
{
    public interface IShipShieldModule : IShipModule
    {
        int MaxShield { get; }
        int CurrentShield { get; set; }
        float RegenRate { get; }
        float RechargeDelay { get; }
        ShieldState State { get; }
        DamageResistances Resistances { get; }

        bool EnableBoundary { get; }
        Vector2 BoundarySize { get; }
        Vector2 BoundaryOffset { get; }
        int BoundaryResolution { get; }

        ShieldImpactConfig ShieldImpactConfig { get; }
        ShieldVisualConfig VisualConfig { get; }

        float AbsorbDamage(V2DamageInfo damageInfo);
        void RestoreShields();

        event Action OnShieldDestroyed;
        event Action OnShieldRestored;
    }
}
