using UnityEngine;

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

    }

    public class Planet
    {
        private GameObject planetObject;

        private PlanetType planetType = PlanetType.None;
        private float orbitDistance;

        public bool HasPlanetObject { get => planetObject != null; }
        public PlanetType GetPlanetType { get => planetType; }
        public float OrbitDistance { get => orbitDistance; }

        public Planet(PlanetType type, float distance)
        {
            planetType = type;
            orbitDistance = distance;
        }

        public GameObject SetPlanetObject(Vector2Int _worldChunkkey, Vector2 _starPosition)
        {
            if (!HasPlanetObject)
            {
                planetObject = ChunkManager.Instance.PlanetGenerator.GetPlanetPool.Get();
                planetObject.transform.SetParent(ChunkManager.Instance.Chunks[_worldChunkkey].ChunkObject.transform);
            }

            ICelestialBody planetController = planetObject.GetComponent<ICelestialBody>();
            SetPlanetProperties(_starPosition);

            return planetObject;
        }

        private void SetPlanetProperties(Vector2 _starPosition)
        {
            planetObject.transform.position = _starPosition + new Vector2(orbitDistance, 0);
        }
    }
}