using System;

namespace StarfireV2
{
    public interface IShipHullModule : IShipModule
    {
        float MaxIntegrity { get; }
        float CurrentIntegrity { get; set; }

        float MaxTemperature { get; }
        float CurrentTemperature { get; set; }

        bool IsDestroyed { get; set; }

        float DamageIntegrity(float damageAmount);
        float RepairIntegrity(float repairAmount);

        event Action OnHullDestroyed;
        event Action<float> OnHullDamaged;
    }
}