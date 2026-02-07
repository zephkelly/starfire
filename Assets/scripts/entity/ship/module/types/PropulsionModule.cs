namespace Starfire.Entity
{
    public class ShipPropulsionModule : IShipModule
    {
        public string ModuleId { get; private set; }
        public ShipModuleCategory Category => ShipModuleCategory.Propulsion;

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; set; }
        public float HealthCoefficient => CurrentHealth / MaxHealth;
        public float EfficiencyMultiplier => HealthCoefficient;
        public bool IsEnabled { get; set; } = true;

        public float MaxSpeed { get; set; }
        public float Acceleration { get; set; }
        public float CurrentDrag { get; set; } = 0.0f;

        public void Damage(float amount)
        {
            if (!IsEnabled) return;
            
            CurrentHealth -= amount;
            if (CurrentHealth < 0) CurrentHealth = 0;
        }
        
        public void Repair(float amount)
        {
            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth)
                CurrentHealth = MaxHealth;
        }
    }
}