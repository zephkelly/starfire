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
        public Chunk ParentChunk { get; private set; }
        public PlanetType PlanetType { get; private set; }
        public float OrbitDistance { get; private set; }

        private CelestialBehaviour _planetCelestialBehaviour;
        private GameObject _planetObject;
        private Rigidbody2D _planetRigid2D;

        // Last position the planet was in while active
        [SerializeField] private Vector2 lastActivePosition = Vector2.zero;
        [SerializeField] private Vector2 lastActiveVelocity = Vector2.zero;
        [SerializeField] private float lastActiveTime = 0f;

        // Getters
        public Rigidbody2D GetRigidbody { get => _planetRigid2D; }
        public bool HasPlanetObject { get => _planetObject != null; }

        public Planet(Chunk parentChunk, PlanetType type, float orbitDistance)
        {
            ParentChunk = parentChunk;
            PlanetType = type;
            OrbitDistance = orbitDistance;

            _planetCelestialBehaviour = null;
            _planetObject = null;
            _planetRigid2D = null;
        }

        public GameObject SetPlanetObject(Vector2 _starPosition)
        {
            if (HasPlanetObject) return null;

            _planetObject = PlanetGenerator.Instance.GetPlanetObject(PlanetType);
            _planetRigid2D = _planetObject.GetComponent<Rigidbody2D>();
            _planetCelestialBehaviour = _planetObject.GetComponent<CelestialBehaviour>();

            if (ParentChunk.ChunkObject == null)
            {
                Debug.LogError("Chunk object is null");
                return null;
            }

            _planetObject.transform.SetParent(ParentChunk.ChunkObject.transform);

            if (lastActivePosition != Vector2.zero)
            {
                _planetObject.transform.position = _starPosition + EstimatePlanetPosition();
            }
            else
            {
                float randomAngle = Random.Range(0, 360);

                Vector2 angleToOrbitalPosition = new Vector2(
                    Mathf.Cos(randomAngle) * OrbitDistance,
                    Mathf.Sin(randomAngle) * OrbitDistance
                );

                _planetObject.transform.position = _starPosition + angleToOrbitalPosition;
            }

            return _planetObject;
        }

        public void RemovePlanetObject()
        {
            if (_planetObject == null) return;

            // Set last active stats
            lastActiveTime = Time.time;
            lastActiveVelocity = _planetRigid2D.velocity;
            Debug.Log(_planetRigid2D == null);
            Debug.Log(_planetRigid2D.velocity);
            lastActivePosition = (Vector2)_planetObject.transform.position - _planetCelestialBehaviour.ParentOrbitingBody.WorldPosition;

            // Release object
            PlanetGenerator.Instance.ReturnPlanetObject(PlanetType, _planetObject);
            _planetObject = null;
            _planetRigid2D = null;
            _planetCelestialBehaviour = null;
        }

        private Vector2 EstimatePlanetPosition()
        {
            // use the last active position and velocity to estimate the current position
            float timeSinceLastActive = Time.time - lastActiveTime;
            Vector2 lastOrbitDirection = lastActivePosition.normalized;

            // use the OrbitDistance and rotate the lastOrbitDirection around the star based on how much time has elapsed
            float rotationPeriod = 2 * Mathf.PI * OrbitDistance / lastActiveVelocity.magnitude;
            float angle = (timeSinceLastActive / rotationPeriod) * 2 * Mathf.PI;

            Vector2 newOrbitDirection = new Vector2(
                lastOrbitDirection.x * Mathf.Cos(angle) + lastOrbitDirection.y * Mathf.Sin(angle),
                -lastOrbitDirection.x * Mathf.Sin(angle) + lastOrbitDirection.y * Mathf.Cos(angle)
            );

            return newOrbitDirection * OrbitDistance;
        }
    }
}