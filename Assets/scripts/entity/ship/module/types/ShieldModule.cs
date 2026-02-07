namespace Starfire.Entity
{
    public class ShipShieldModule : IShipModule
    {
        public string ModuleId { get; private set; }
        public ShipModuleCategory Category => ShipModuleCategory.Defense;

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; set; }
        public float HealthCoefficient => 1.0f - (CurrentHealth / MaxHealth);
        public float EfficiencyMultiplier { get; set; } = 1.0f;
        public bool IsEnabled { get; set; } = true;

        public float MaxTemperature { get; set; }
        public float CurrentTemperature { get; set; }

        public float RegenerationRate { get; set; } = 1.0f;
        public float RegenerationDelay { get; set; } = 5.0f;
        public float RegenerationMinimumHealthThreshold { get; set; } = 0.2f;
        public float LastDamageTime { get; private set; }

        public ShipShieldModule(string moduleId, float maxHealth)
        {
            ModuleId = moduleId;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;
            
            LastDamageTime += deltaTime;
            
            if (CurrentHealth < MaxHealth && LastDamageTime >= RegenerationDelay && HealthCoefficient <= RegenerationMinimumHealthThreshold)
            {
                CurrentHealth += RegenerationRate * deltaTime;
                if (CurrentHealth > MaxHealth)
                {
                    CurrentHealth = MaxHealth;
                }
            }
        }
        
        public void Damage(float amount)
        {
            if (!IsEnabled) return;
            
            CurrentHealth -= amount;
            if (CurrentHealth < 0) CurrentHealth = 0;
            
            LastDamageTime = 0;
        }

        public void Repair(float amount)
        {
            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
        }
    }
}