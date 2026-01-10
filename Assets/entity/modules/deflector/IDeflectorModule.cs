namespace Starfire.Entity.Modules.Deflector
{
    public interface IDeflectorModule : IEntityModule
    {
        float DeflectionStrength { get; }
        float BeamFocusMultiplier { get; }
    }
}
