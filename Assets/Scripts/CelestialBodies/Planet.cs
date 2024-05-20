namespace Starfire
{
    public enum PlanetType
    {
        None,
        Rivers,
        Land,
        Gas,
        GasLayers,
        Ice,
        Lava,
        Desert,
        Moon
    }

    public interface IPlanet 
    {
        PlanetType PlanetType { get; }
        void SetPlanetType(PlanetType type);
    }
}