using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Handles the visual and audio effects when a projectile impacts a target.
    /// Self-destructs after the configured duration.
    /// </summary>
    public class ImpactEffect : MonoBehaviour
    {
        private ImpactConfig _config;
        private Light2D _light;
        private float _startTime;
        private float _initialIntensity;

        /// <summary>
        /// Factory method to spawn an impact effect at a position.
        /// </summary>
        public static ImpactEffect Spawn(ImpactConfig config, Vector2 position, Vector2 direction)
        {
            if (config == null) return null;

            var go = new GameObject("ImpactEffect");
            go.transform.position = position;

            // Rotate to face the impact direction (opposite of projectile direction)
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            var effect = go.AddComponent<ImpactEffect>();
            effect.Initialize(config);

            return effect;
        }

        /// <summary>
        /// Factory method to spawn an impact effect with reflection particles for shield impacts.
        /// </summary>
        /// <param name="config">Impact configuration</param>
        /// <param name="position">Impact position</param>
        /// <param name="direction">Direction facing away from impact surface</param>
        /// <param name="incomingVelocity">Velocity of the incoming projectile (for reflection calculation)</param>
        /// <param name="surfaceNormal">Normal of the surface hit (for reflection calculation)</param>
        public static ImpactEffect Spawn(
            ImpactConfig config,
            Vector2 position,
            Vector2 direction,
            Vector2 incomingVelocity,
            Vector2 surfaceNormal)
        {
            if (config == null) return null;

            var go = new GameObject("ImpactEffect");
            go.transform.position = position;

            // Rotate to face the impact direction
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            var effect = go.AddComponent<ImpactEffect>();
            effect.Initialize(config, incomingVelocity, surfaceNormal);

            return effect;
        }

        private void Initialize(ImpactConfig config)
        {
            Initialize(config, Vector2.zero, Vector2.zero);
        }

        private void Initialize(ImpactConfig config, Vector2 incomingVelocity, Vector2 surfaceNormal)
        {
            _config = config;
            _startTime = Time.time;

            // Spawn particle effect
            if (_config.particlePrefab != null)
            {
                var particles = Instantiate(_config.particlePrefab, transform.position, transform.rotation, transform);
                particles.transform.localScale = Vector3.one * _config.particleScale;
            }

            // Spawn reflection particles if enabled and we have velocity data
            if (_config.enableReflection && incomingVelocity.sqrMagnitude > 0.01f)
            {
                CreateReflectionParticles(incomingVelocity, surfaceNormal);
            }

            // Create light
            if (_config.enableLight)
            {
                var lightGO = new GameObject("ImpactLight");
                lightGO.transform.SetParent(transform);
                lightGO.transform.localPosition = Vector3.zero;

                _light = lightGO.AddComponent<Light2D>();
                _light.lightType = Light2D.LightType.Point;
                _light.color = _config.lightColor;
                _light.intensity = _config.lightIntensity;
                _light.pointLightOuterRadius = _config.lightRadius;
                _light.pointLightInnerRadius = _config.lightRadius * 0.1f;

                _initialIntensity = _config.lightIntensity;
            }

            // Play sound
            if (_config.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(_config.impactSound, transform.position, _config.soundVolume);
            }
        }

        private void CreateReflectionParticles(Vector2 incomingVelocity, Vector2 surfaceNormal)
        {
            // Calculate reflection direction: R = V - 2(V.N)N
            Vector2 incomingDir = incomingVelocity.normalized;
            Vector2 reflectionDir = incomingDir - 2f * Vector2.Dot(incomingDir, surfaceNormal) * surfaceNormal;
            reflectionDir.Normalize();

            // Create particle system GameObject
            var particleGO = new GameObject("ReflectionParticles");
            particleGO.transform.SetParent(transform);
            particleGO.transform.localPosition = Vector3.zero;

            var ps = particleGO.AddComponent<ParticleSystem>();
            ConfigureReflectionParticleSystem(ps);

            // Emit particles manually with calculated velocities for precise direction control
            float speed = _config.reflectionSpeed * _config.reflectionIntensity;
            float spreadRad = _config.reflectionSpreadAngle * Mathf.Deg2Rad;

            var emitParams = new ParticleSystem.EmitParams();
            emitParams.startLifetime = _config.reflectionLifetime;
            emitParams.startSize = _config.reflectionTrailWidth;
            emitParams.startColor = _config.reflectionColor;

            for (int i = 0; i < _config.reflectionParticleCount; i++)
            {
                // Random angle within spread cone
                float randomAngle = Random.Range(-spreadRad, spreadRad);

                // Rotate reflection direction by random angle (2D rotation)
                float cos = Mathf.Cos(randomAngle);
                float sin = Mathf.Sin(randomAngle);
                Vector2 particleDir = new Vector2(
                    reflectionDir.x * cos - reflectionDir.y * sin,
                    reflectionDir.x * sin + reflectionDir.y * cos
                );

                // Random speed variation
                float particleSpeed = speed * Random.Range(
                    1f - _config.reflectionSpeedVariation,
                    1f + _config.reflectionSpeedVariation);

                emitParams.velocity = particleDir * particleSpeed;
                ps.Emit(emitParams, 1);
            }
        }

        private void ConfigureReflectionParticleSystem(ParticleSystem ps)
        {
            // Main module
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false; // We emit manually
            main.startLifetime = _config.reflectionLifetime;
            main.startSpeed = 0f; // Velocity set per-particle via EmitParams
            main.startSize = _config.reflectionTrailWidth;
            main.startColor = _config.reflectionColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = _config.reflectionParticleCount;

            // Disable emission - we emit manually with calculated velocities
            var emission = ps.emission;
            emission.enabled = false;

            // Disable shape - we set velocity directly per particle
            var shape = ps.shape;
            shape.enabled = false;

            // Size over lifetime - slight shrink for trail taper
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f));

            // Color over lifetime - fade out
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(_config.reflectionColor, 0f),
                    new GradientColorKey(_config.reflectionColor, 0.5f),
                    new GradientColorKey(_config.reflectionColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Trails module for elongated streak effect
            var trails = ps.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(_config.reflectionTrailLengthMultiplier);
            trails.minVertexDistance = 0.05f;
            trails.worldSpace = true;
            trails.dieWithParticles = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            trails.colorOverLifetime = gradient;
            trails.inheritParticleColor = true;

            // Renderer configuration
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 50;

            // Create additive material for glowing particles
            var material = new Material(Shader.Find("Sprites/Default"));
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            renderer.material = material;

            // Trail material
            var trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            renderer.trailMaterial = trailMaterial;

            // Play the system
            ps.Play();
        }

        private void Update()
        {
            float elapsed = Time.time - _startTime;

            // Update light fade
            if (_light != null && elapsed >= _config.fadeStartTime)
            {
                float fadeProgress = (elapsed - _config.fadeStartTime) / (_config.duration - _config.fadeStartTime);
                fadeProgress = Mathf.Clamp01(fadeProgress);

                float curveValue = _config.fadeCurve.Evaluate(fadeProgress);
                _light.intensity = _initialIntensity * (1f - curveValue);
            }

            // Self-destruct after duration
            if (elapsed >= _config.duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
