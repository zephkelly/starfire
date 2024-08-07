namespace Starfire
{
    public interface ICelestialBody
    {
        public CelestialBodyType CelestialBodyType { get; }
        public OrbitingController OrbitController { get; }
        // public float MaxOrbitRadius { get; }
        public float Temperature { get; }
        CelestialBehaviour ParentOrbitingBody { get; }
        CelestialBehaviour ChildOrbitingBody { get; }
        bool IsOrbiting { get; }
        float Mass { get; }
        string Name { get; }
        void SetupCelestialBehaviour(CelestialBodyType type, float mass, float radius, string name);
        void SetOrbitingBody(CelestialBehaviour orbitingBody);
        void RemoveOrbitingBody();
    }
}