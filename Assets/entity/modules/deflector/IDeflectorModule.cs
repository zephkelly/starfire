namespace Starfire.Entity.Modules.Deflector
{
    public interface IDeflectorModule : IShipModule
    {
        float DeflectionStrength { get; }
        float BeamFocusMultiplier { get; }
    }
}
