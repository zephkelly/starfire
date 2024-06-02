namespace Starfire
{
    public interface ICelestialBody
    {
        CelestialBodyType CelestialBodyType { get; }
        OrbitingController OrbitController { get; }
        float MaxOrbitRadius { get; }
        float Temperature { get; }
        CelestialBehaviour ParentOrbitingBody { get; }
        CelestialBehaviour ChildOrbitingBody { get; }
        bool IsOrbiting { get; }
        float Mass { get; }
        string CelestialName { get; }
        void SetupCelestialBehaviour(CelestialBodyType type, int radius, string name);
        void SetOrbitingBody(CelestialBehaviour orbitingBody);
        void RemoveOrbitingBody();
    }
}