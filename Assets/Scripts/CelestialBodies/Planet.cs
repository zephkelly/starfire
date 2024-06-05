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

    public class Planet
    {
        private GameObject planetObject = null;
        private CelestialBehaviour planetCelestialBehaviour = null;
        private Rigidbody2D planetRigidbody = null;
        private PlanetType planetType = PlanetType.None;
        private float orbitDistance;

        // Last position the planet was in while active
        private Vector2 lastActivePosition = Vector2.zero;
        private Vector2 lastActiveVelocity = Vector2.zero;
        private float lastActiveTime = 0f;

        public Rigidbody2D GetPlanetRigidbody { get => planetRigidbody; }
        public bool HasPlanetObject { get => planetObject != null; }
        public PlanetType GetPlanetType { get => planetType; }
        public float OrbitDistance { get => orbitDistance; }

        public Planet(PlanetType type, float distance)
        {
            planetType = type;
            orbitDistance = distance;
        }

        public GameObject SetPlanetObject(Vector2Int _chunkAbsKey, Vector2 _starPosition)
        {
            if (HasPlanetObject) return null;

            planetObject = PlanetGenerator.Instance.GetPlanetObject(planetType);
            planetRigidbody = planetObject.GetComponent<Rigidbody2D>();
            planetCelestialBehaviour = planetObject.GetComponent<CelestialBehaviour>();

            GameObject chunkObject = GetChunk(_chunkAbsKey).ChunkObject;

            if (chunkObject == null)
            {
                Debug.LogError("Chunk object is null");
                return null;
            }

            planetObject.transform.SetParent(chunkObject.transform);
            planetObject.transform.position = _starPosition + new Vector2(orbitDistance, 0);

            return planetObject;
        }

        public void RemovePlanetObject()
        {
            if (planetObject == null) return;

            // Set last active stats
            lastActiveTime = Time.time;
            lastActiveVelocity = planetRigidbody.velocity;

            Vector2 planetDistance = (Vector2)planetObject.transform.position - planetCelestialBehaviour.ParentOrbitingBody.WorldPosition;
            lastActivePosition = planetDistance;

            // Release object
            PlanetGenerator.Instance.ReturnPlanetObject(planetType, planetObject);
            planetObject = null;
            planetRigidbody = null;
            planetCelestialBehaviour = null;
        }

        public Chunk GetChunk(Vector2Int _worldChunkKey)
        {
            return ChunkManager.Instance.Chunks[_worldChunkKey];
        }
    }
}