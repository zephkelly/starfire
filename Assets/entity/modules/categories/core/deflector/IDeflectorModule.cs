namespace Starfire.Entity.Modules.Deflector
{
    public interface IDeflectorShipModule : IShipModule
    {
        float DeflectionStrength { get; }
        float BeamFocusMultiplier { get; }
    }
}
