namespace Starfire.Entity
{
    public interface IShipModule
    {
        string ModuleId { get; }
        ShipModuleCategory Category { get; }

        float MaxHealth { get; }
        float CurrentHealth { get; set; }
        float HealthCoefficient { get; }
        float EfficiencyMultiplier { get; }
        bool IsEnabled { get; set; }

        public void Update(float deltaTime) {}

        public void Damage(float amount) { }

        public void Repair(float amount) { }
    }
}