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

        private ParticleSystem _particleSystem;
        private ParticleSystem.EmitParams _emitParams;
        private Camera _mainCamera;
        private float _emissionAccumulator;
        private Vector2 _lastVelocityDir;

        // Shader property IDs
        private static readonly int WarpIntensityId = Shader.PropertyToID("_WarpIntensity");
        private static readonly int WarpDirectionId = Shader.PropertyToID("_WarpDirection");

        void Start()
        {
            _mainCamera = Camera.main;
            CreateParticleSystem();
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

        void OnDestroy()
        {
            if (_particleSystem != null)
            {
                Destroy(_particleSystem.gameObject);
            }
        }
    }
}
