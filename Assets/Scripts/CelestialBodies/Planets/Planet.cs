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
        public float Radius { get; private set; }
        public float Mass { get; private set; }

        private CelestialBehaviour _planetCelestialBehaviour;
        private GameObject _planetObject;
        private Rigidbody2D _planetRigid2D;
        private Vector2 _planetOrbitPosition;

        [SerializeField] private string planetName;

        // Last position the planet was in while active
        [SerializeField] private Vector2 lastActivePosition = Vector2.zero;
        [SerializeField] private Vector2 lastActiveVelocity = Vector2.zero;
        [SerializeField] private float lastActiveTime = 0f;

        public Rigidbody2D GetRigidbody { get => _planetRigid2D; }
        public bool HasPlanetObject { get => _planetObject != null; }

        public Planet(Chunk parentChunk, PlanetType type, float orbitDistance, float mass)
        {
            ParentChunk = parentChunk;
            PlanetType = type;
            OrbitDistance = orbitDistance;
            Mass = mass;

            planetName = "Boba";
            

            _planetCelestialBehaviour = null;
            _planetObject = null;
            _planetRigid2D = null;

            // Set random start orbit position
            float randomAngle = Random.Range(0, 360);

            _planetOrbitPosition = new Vector2(
                Mathf.Cos(randomAngle) * OrbitDistance,
                Mathf.Sin(randomAngle) * OrbitDistance
            );

            lastActivePosition = _planetOrbitPosition;
            lastActiveVelocity = EstimateOrbitalVelocity();
            lastActiveTime = Time.time;
        }

        public Vector2 GetOrbitPosition()
        {
            _planetOrbitPosition = EstimatePlanetPosition();
            return ParentChunk.GetStar.GetStarPosition + _planetOrbitPosition;
        }

        public GameObject SetPlanetObject(Vector2 _starPosition)
        {
            if (HasPlanetObject) return null;

            _planetObject = PlanetGenerator.Instance.GetPlanetObject(PlanetType);
            _planetObject.transform.SetParent(ParentChunk.ChunkObject.transform);
            Radius = _planetObject.GetComponent<Collider2D>().bounds.size.x / 2f;

            _planetRigid2D = _planetObject.GetComponent<Rigidbody2D>();
            _planetRigid2D.mass = Mass;

            _planetCelestialBehaviour = _planetObject.GetComponent<CelestialBehaviour>();
            _planetCelestialBehaviour.SetupCelestialBehaviour(CelestialBodyType.Star, Radius, Mass, planetName);

            int visualSize = Mathf.RoundToInt(Radius);
            _planetCelestialBehaviour.NewPixelAmount(visualSize);

            if (ParentChunk.ChunkObject == null)
            {
                Debug.LogError("Chunk object is null");
                return null;
            }

            if (lastActivePosition != Vector2.zero)
            {
                _planetObject.transform.position = GetOrbitPosition();
            }
            else
            {
                _planetObject.transform.position = _starPosition + _planetOrbitPosition;
            }

            CameraController.Instance.starParallaxLayers.Add(_planetCelestialBehaviour.GetParallaxLayer);

            return _planetObject;
        }

        public void RemovePlanetObject()
        {
            if (_planetObject == null) return;
            if (_planetCelestialBehaviour == null) return;
            if (_planetCelestialBehaviour.ParentOrbitingBody == null) return;

            // Set last active stats
            lastActiveTime = Time.time;
            lastActiveVelocity = _planetRigid2D.velocity;
            lastActivePosition = (Vector2)_planetObject.transform.position - _planetCelestialBehaviour.ParentOrbitingBody.WorldPosition;

            // Release object
            PlanetGenerator.Instance.ReturnPlanetObject(PlanetType, _planetObject);
            _planetObject = null;
            _planetRigid2D = null;
            _planetCelestialBehaviour = null;
        }

        public Vector2 EstimatePlanetPosition()
        {
            // Use OrbitDistance to rotate lastOrbitDirection around the star by last active time
            Vector2 lastOrbitDirection = lastActivePosition.normalized;
            float timeSinceLastActive = Time.time - lastActiveTime;
            float rotationPeriod = 2 * Mathf.PI * OrbitDistance / lastActiveVelocity.magnitude;
            float angle = (timeSinceLastActive / rotationPeriod) * 2 * Mathf.PI;

            Vector2 newOrbitDirection = new Vector2(
                lastOrbitDirection.x * Mathf.Cos(angle) + lastOrbitDirection.y * Mathf.Sin(angle),
                -lastOrbitDirection.x * Mathf.Sin(angle) + lastOrbitDirection.y * Mathf.Cos(angle)
            );

            lastActivePosition = newOrbitDirection * OrbitDistance;
            lastActiveTime = Time.time;

            return lastActivePosition;
        }

        public Vector2 EstimateOrbitalVelocity()
        {
            //Parent orbiting controller
            Star parentStar = ParentChunk.GetStar;

            float parentStarMass = parentStar.Mass * 10;

            // Calculate the distance from the planet to the central body
            float distanceToStar = Vector2.Distance(Vector2.zero, GetOrbitPosition());
            
            // Calculate the direction from the planet to the star
            Vector2 directionToStar = (Vector2.zero - GetOrbitPosition()).normalized;
            
            // Calculate the perpendicular direction to the direction to the star
            Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);
            
            // Calculate the orbital velocity magnitude using the formula
            float orbitalVelocityMagnitude = Mathf.Sqrt(0.2f * (parentStarMass) / distanceToStar);
            
            // Calculate the orbital velocity vector
            Vector2 orbitalVelocity = perpendicularDirection * orbitalVelocityMagnitude;

            // Add the velocity of the central body (if it's moving)

            return orbitalVelocity;
        }
    }
}