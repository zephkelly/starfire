using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// Creates a particle system that emits particles along the wake bubble edge,
    /// streaming behind the ship to enhance the warp effect visibility.
    /// Attach this to the main camera or a child object that follows the ship.
    /// </summary>
    public class WakeBubbleParticles : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Configuration for bubble parameters")]
        public GravitationalWakeConfig wakeConfig;

        [Tooltip("Transform to follow (usually the player ship)")]
        public Transform target;

        [Header("Particle Appearance")]
        [Tooltip("Primary color of particles")]
        public Color particleColor = new Color(0.3f, 0.8f, 1f, 0.8f);

        [Tooltip("Secondary color for gradient")]
        public Color particleColorEnd = new Color(0.1f, 0.4f, 1f, 0f);

        [Tooltip("Particle size range")]
        public Vector2 particleSizeRange = new Vector2(0.1f, 0.3f);

        [Header("Emission")]
        [Tooltip("Particles emitted per second")]
        [Range(10f, 200f)] public float emissionRate = 80f;

        [Tooltip("Particle lifetime in seconds")]
        [Range(0.5f, 3f)] public float particleLifetime = 1.5f;

        [Tooltip("Base speed of particles along bubble edge")]
        [Range(0.5f, 5f)] public float tangentSpeed = 2f;

        [Tooltip("Outward drift speed")]
        [Range(0f, 2f)] public float radialDrift = 0.5f;

        [Header("Behavior")]
        [Tooltip("Minimum warp intensity to emit particles")]
        [Range(0f, 0.5f)] public float activationThreshold = 0.1f;

        [Tooltip("Bias particles toward the wake (behind ship). 0 = uniform, 1 = only behind")]
        [Range(0f, 1f)] public float wakeBias = 0.7f;

        [Header("Front Deflector Particles")]
        [Tooltip("Enable particles at the front deflector point")]
        public bool enableDeflectorParticles = true;

        [Tooltip("Emission rate for deflector particles")]
        [Range(5f, 50f)] public float deflectorEmissionRate = 20f;

        [Tooltip("Color of deflector particles")]
        public Color deflectorColor = new Color(0.6f, 0.9f, 1f, 1f);

        [Tooltip("How particles spread outward from deflector (degrees)")]
        [Range(0f, 90f)] public float deflectorSpreadAngle = 45f;

        [Tooltip("Speed of deflector particles")]
        [Range(1f, 10f)] public float deflectorParticleSpeed = 5f;

        [Header("Sonic Barrier Debris")]
        [Tooltip("Enable larger debris particles being pushed aside during high-speed warp")]
        public bool enableDebrisParticles = true;

        [Tooltip("Debris emission rate")]
        [Range(5f, 50f)] public float debrisEmissionRate = 15f;

        [Tooltip("Size range for debris particles (larger than regular particles)")]
        public Vector2 debrisSizeRange = new Vector2(0.3f, 0.8f);

        [Tooltip("How fast debris is pushed away from ship")]
        [Range(2f, 15f)] public float debrisPushSpeed = 8f;

        [Tooltip("Debris particle lifetime")]
        [Range(0.5f, 3f)] public float debrisLifetime = 2f;

        [Tooltip("Debris particle color")]
        public Color debrisColor = new Color(0.6f, 0.7f, 0.8f, 0.9f);

        [Tooltip("Debris rotation speed range (degrees/sec)")]
        public Vector2 debrisRotationSpeed = new Vector2(-180f, 180f);

        [Tooltip("Concentration toward front (0 = uniform, 1 = only at front piercing point)")]
        [Range(0f, 1f)] public float debrisFrontConcentration = 0.6f;

        [Tooltip("Warp intensity threshold to start emitting debris (higher = barrier breaking moment)")]
        [Range(0f, 0.5f)] public float debrisActivationThreshold = 0.3f;

        [Tooltip("Optional sprite for debris particles (uses default circle if null)")]
        public Sprite debrisSprite;

        private ParticleSystem _particleSystem;
        private ParticleSystem _debrisParticleSystem;
        private ParticleSystem.EmitParams _emitParams;
        private Camera _mainCamera;
        private float _emissionAccumulator;
        private float _deflectorAccumulator;
        private float _debrisAccumulator;
        private Vector2 _lastVelocityDir;

        // Shader property IDs
        private static readonly int WarpIntensityId = Shader.PropertyToID("_WarpIntensity");
        private static readonly int WarpDirectionId = Shader.PropertyToID("_WarpDirection");

        void Start()
        {
            _mainCamera = Camera.main;
            CreateParticleSystem();
            CreateDebrisParticleSystem();
        }

        void Update()
        {
            if (target == null || _particleSystem == null || wakeConfig == null) return;

            float warpIntensity = Shader.GetGlobalFloat(WarpIntensityId);

            if (warpIntensity < activationThreshold)
            {
                return;
            }

            Vector4 warpDirVec = Shader.GetGlobalVector(WarpDirectionId);
            Vector2 warpDir = new Vector2(warpDirVec.x, warpDirVec.y);
            if (warpDir.sqrMagnitude > 0.001f)
            {
                _lastVelocityDir = warpDir.normalized;
            }

            EmitAlongBubbleEdge(warpIntensity, _lastVelocityDir);
            EmitDeflectorParticles(warpIntensity, _lastVelocityDir);
            EmitDebrisParticles(warpIntensity, _lastVelocityDir);
        }

        void CreateParticleSystem()
        {
            GameObject particleObj = new GameObject("WakeBubbleParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.zero;

            _particleSystem = particleObj.AddComponent<ParticleSystem>();

            var main = _particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = particleLifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(particleSizeRange.x, particleSizeRange.y);
            main.startColor = particleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;

            var emission = _particleSystem.emission;
            emission.enabled = false;

            var colorOverLifetime = _particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(particleColor, 0f),
                    new GradientColorKey(particleColorEnd, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(particleColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = _particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0.3f)
            ));

            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 10;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = particleColor;

            _emitParams = new ParticleSystem.EmitParams();

            _particleSystem.Play();
        }

        void EmitAlongBubbleEdge(float warpIntensity, Vector2 warpDir)
        {
            float adjustedEmissionRate = emissionRate * warpIntensity;
            _emissionAccumulator += adjustedEmissionRate * Time.deltaTime;

            int particlesToEmit = Mathf.FloorToInt(_emissionAccumulator);
            _emissionAccumulator -= particlesToEmit;

            if (particlesToEmit <= 0) return;

            float bubbleRadius = wakeConfig.bubbleRadius;
            float ellipseRatio = wakeConfig.ellipseRatio;

            float orthoSize = _mainCamera.orthographicSize;
            float aspect = _mainCamera.aspect;
            float worldBubbleRadius = bubbleRadius * orthoSize * 2f;

            Vector3 shipPos = target.position;

            for (int i = 0; i < particlesToEmit; i++)
            {
                float angle = GetBiasedAngle(warpDir);

                float radiusAtAngle = worldBubbleRadius;
                if (ellipseRatio < 1f)
                {
                    Vector2 majorDir = warpDir;
                    Vector2 minorDir = new Vector2(-warpDir.y, warpDir.x);
                    float majorComponent = Mathf.Cos(angle) * warpDir.x + Mathf.Sin(angle) * warpDir.y;
                    float minorComponent = Mathf.Cos(angle) * (-warpDir.y) + Mathf.Sin(angle) * warpDir.x;
                    float ellipseScale = Mathf.Sqrt(majorComponent * majorComponent + (minorComponent * minorComponent) / (ellipseRatio * ellipseRatio));
                    if (ellipseScale > 0.001f)
                    {
                        radiusAtAngle = worldBubbleRadius / ellipseScale;
                    }
                }

                Vector2 offsetDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 emitPos = shipPos + new Vector3(offsetDir.x, offsetDir.y, 0) * radiusAtAngle;

                Vector2 tangent = new Vector2(-offsetDir.y, offsetDir.x);
                float tangentSign = Vector2.Dot(tangent, -warpDir) > 0 ? 1f : -1f;
                tangent *= tangentSign;

                Vector2 velocity = tangent * tangentSpeed + offsetDir * radialDrift;
                velocity -= warpDir * tangentSpeed * 0.5f;

                _emitParams.position = emitPos;
                _emitParams.velocity = new Vector3(velocity.x, velocity.y, 0);
                _emitParams.startSize = Random.Range(particleSizeRange.x, particleSizeRange.y);
                _emitParams.startLifetime = particleLifetime * Random.Range(0.8f, 1.2f);
                _emitParams.startColor = Color.Lerp(particleColor, particleColorEnd, Random.value * 0.3f);

                _particleSystem.Emit(_emitParams, 1);
            }
        }

        float GetBiasedAngle(Vector2 warpDir)
        {
            float baseAngle = Random.Range(0f, Mathf.PI * 2f);

            if (wakeBias > 0.01f)
            {
                float warpAngle = Mathf.Atan2(warpDir.y, warpDir.x);
                float behindAngle = warpAngle + Mathf.PI;

                float angleDiff = Mathf.DeltaAngle(baseAngle * Mathf.Rad2Deg, behindAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                baseAngle = Mathf.LerpAngle(baseAngle * Mathf.Rad2Deg, (behindAngle + angleDiff * (1f - wakeBias)) * Mathf.Rad2Deg, wakeBias) * Mathf.Deg2Rad;
            }

            return baseAngle;
        }

        void EmitDeflectorParticles(float warpIntensity, Vector2 warpDir)
        {
            if (!enableDeflectorParticles) return;

            float adjustedRate = deflectorEmissionRate * warpIntensity;
            _deflectorAccumulator += adjustedRate * Time.deltaTime;

            int count = Mathf.FloorToInt(_deflectorAccumulator);
            _deflectorAccumulator -= count;

            if (count <= 0) return;

            // Calculate front point position
            float orthoSize = _mainCamera.orthographicSize;
            float worldRadius = wakeConfig.bubbleRadius * orthoSize * 2f;
            Vector3 shipPos = target.position;
            Vector3 frontPoint = shipPos + new Vector3(warpDir.x, warpDir.y, 0) * worldRadius;

            for (int i = 0; i < count; i++)
            {
                // Spread particles in cone from front point
                float spreadRad = deflectorSpreadAngle * Mathf.Deg2Rad;
                float randomAngle = Random.Range(-spreadRad, spreadRad);

                // Rotate warp direction by random angle
                float cos = Mathf.Cos(randomAngle);
                float sin = Mathf.Sin(randomAngle);
                Vector2 particleDir = new Vector2(
                    warpDir.x * cos - warpDir.y * sin,
                    warpDir.x * sin + warpDir.y * cos
                );

                _emitParams.position = frontPoint;
                _emitParams.velocity = new Vector3(particleDir.x, particleDir.y, 0) * deflectorParticleSpeed;
                _emitParams.startSize = Random.Range(particleSizeRange.x * 0.5f, particleSizeRange.y * 0.8f);
                _emitParams.startLifetime = particleLifetime * 0.6f;
                _emitParams.startColor = deflectorColor;

                _particleSystem.Emit(_emitParams, 1);
            }
        }

        void CreateDebrisParticleSystem()
        {
            if (!enableDebrisParticles) return;

            GameObject debrisObj = new GameObject("WakeDebrisParticles");
            debrisObj.transform.SetParent(transform);
            debrisObj.transform.localPosition = Vector3.zero;

            _debrisParticleSystem = debrisObj.AddComponent<ParticleSystem>();

            var main = _debrisParticleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = debrisLifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(debrisSizeRange.x, debrisSizeRange.y);
            main.startColor = debrisColor;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            // Add rotation over lifetime for tumbling debris effect
            var rotation = _debrisParticleSystem.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(
                debrisRotationSpeed.x * Mathf.Deg2Rad,
                debrisRotationSpeed.y * Mathf.Deg2Rad
            );

            // Size over lifetime - slight growth then shrink
            var sizeOverLifetime = _debrisParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.1f, 1f),
                new Keyframe(0.7f, 0.8f),
                new Keyframe(1f, 0f)
            ));

            // Color fade over lifetime
            var colorOverLifetime = _debrisParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(debrisColor, 0f),
                    new GradientColorKey(debrisColor, 0.5f),
                    new GradientColorKey(debrisColor * 0.5f, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(debrisColor.a, 0.1f),
                    new GradientAlphaKey(debrisColor.a * 0.5f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var emission = _debrisParticleSystem.emission;
            emission.enabled = false;

            var renderer = debrisObj.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 9; // Below main particles (10)

            if (debrisSprite != null)
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.mainTexture = debrisSprite.texture;
            }
            else
            {
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = debrisColor;
            }

            _debrisParticleSystem.Play();
        }

        void EmitDebrisParticles(float warpIntensity, Vector2 warpDir)
        {
            if (!enableDebrisParticles || _debrisParticleSystem == null) return;

            // Only emit above debris activation threshold (barrier breaking moment)
            if (warpIntensity < debrisActivationThreshold) return;

            // Scale emission with intensity above threshold
            float intensityAboveThreshold = (warpIntensity - debrisActivationThreshold) / (1f - debrisActivationThreshold);
            float adjustedRate = debrisEmissionRate * Mathf.Pow(intensityAboveThreshold, 0.5f);
            _debrisAccumulator += adjustedRate * Time.deltaTime;

            int count = Mathf.FloorToInt(_debrisAccumulator);
            _debrisAccumulator -= count;

            if (count <= 0) return;

            float orthoSize = _mainCamera.orthographicSize;
            float worldRadius = wakeConfig.bubbleRadius * orthoSize * 2f;
            Vector3 shipPos = target.position;

            // Calculate front piercing point (tip of the ellipse)
            Vector3 frontPoint = shipPos + new Vector3(warpDir.x, warpDir.y, 0) * worldRadius;

            // Perpendicular direction for tangential flow
            Vector2 perpDir = new Vector2(-warpDir.y, warpDir.x);

            for (int i = 0; i < count; i++)
            {
                // Spawn at front tip with small lateral spread
                float spawnSpread = 0.15f * (1f - debrisFrontConcentration);
                float lateralOffset = Random.Range(-spawnSpread, spawnSpread) * worldRadius;
                Vector3 emitPos = frontPoint + new Vector3(perpDir.x, perpDir.y, 0) * lateralOffset;

                // Add small forward offset so debris spawns slightly ahead of bubble
                emitPos += new Vector3(warpDir.x, warpDir.y, 0) * worldRadius * Random.Range(0f, 0.2f);

                // Velocity: tangential flow around bubble (left or right side randomly)
                float side = Random.value > 0.5f ? 1f : -1f;
                Vector2 tangentDir = perpDir * side;

                // Combine tangential flow with outward push and backward drift
                Vector2 velocity = tangentDir * debrisPushSpeed * 0.8f           // Flow around sides
                                 + perpDir * side * debrisPushSpeed * 0.4f       // Push outward from center
                                 - warpDir * debrisPushSpeed * 0.2f;             // Drift backward in wake

                float speed = Random.Range(0.8f, 1.2f);
                velocity *= speed;

                _emitParams.position = emitPos;
                _emitParams.velocity = new Vector3(velocity.x, velocity.y, 0);
                _emitParams.startSize = Random.Range(debrisSizeRange.x, debrisSizeRange.y);
                _emitParams.startLifetime = debrisLifetime * Random.Range(0.8f, 1.2f);
                _emitParams.startColor = debrisColor;
                _emitParams.rotation = Random.Range(0f, 360f);

                _debrisParticleSystem.Emit(_emitParams, 1);
            }
        }

        void OnDestroy()
        {
            if (_particleSystem != null)
            {
                Destroy(_particleSystem.gameObject);
            }
            if (_debrisParticleSystem != null)
            {
                Destroy(_debrisParticleSystem.gameObject);
            }
        }
    }
}
