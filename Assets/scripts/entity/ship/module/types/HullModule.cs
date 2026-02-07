namespace Starfire.Entity
{
    public class ShipHullModule : IShipModule
    {
        public string ModuleId { get; set; }
        public ShipModuleCategory Category => ShipModuleCategory.Hull;

        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        public bool IsEnabled { get; set; } = true;

        public float MaxTemperature { get; set; }
        public float CurrentTemperature { get; set; }

        public float HealthCoefficient => CurrentHealth / MaxHealth;
        public float EfficiencyMultiplier { get; set; } = 1.0f;

        public void Damage(float amount)
        {
            float effectiveDamage = amount * EfficiencyMultiplier;
            CurrentHealth -= effectiveDamage;
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
                IsEnabled = false;
            }
        }

        public void Repair(float amount)
        {
            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }
        }

        public void Update(float deltaTime) { }
    }
}