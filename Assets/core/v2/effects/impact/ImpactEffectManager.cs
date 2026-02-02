using System.Collections.Generic;
using Starfire.Core.V3.Cam.Effects;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StarfireV2
{
    /// <summary>
    /// Singleton manager for high-performance impact effects.
    /// Uses persistent ParticleSystem instances with manual Emit() for zero-allocation impacts.
    /// Pools Light2D objects for impact flashes.
    /// </summary>
    public class ImpactEffectManager : MonoBehaviour
    {
        public static ImpactEffectManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private ImpactEffectConfig config;

        [Header("Default Presets")]
        [SerializeField] private ImpactPreset defaultHullImpact;
        [SerializeField] private ImpactPreset defaultShieldImpact;
        [SerializeField] private ImpactPreset defaultExplosion;

        // One particle system set per preset, created on demand
        private readonly Dictionary<ImpactPreset, ParticleSystemSet> _particleSystems = new();

        // Pooled lights
        private readonly Queue<ImpactLight> _lightPool = new();
        private readonly List<ImpactLight> _activeLights = new();

        private Transform _particleContainer;
        private Transform _lightContainer;
        private ImpactPreset _fallbackPreset;

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _particleContainer = new GameObject("ImpactParticles").transform;
            _particleContainer.SetParent(transform);

            _lightContainer = new GameObject("ImpactLights").transform;
            _lightContainer.SetParent(transform);

            int poolSize = config != null ? config.lightPoolSize : 50;
            for (int i = 0; i < poolSize; i++)
            {
                var light = CreatePooledLight();
                light.gameObject.SetActive(false);
                _lightPool.Enqueue(light);
            }

            // Pre-warm assigned presets
            if (defaultHullImpact != null) EnsureParticleSystems(defaultHullImpact);
            if (defaultShieldImpact != null) EnsureParticleSystems(defaultShieldImpact);
            if (defaultExplosion != null) EnsureParticleSystems(defaultExplosion);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _activeLights.Count - 1; i >= 0; i--)
            {
                var light = _activeLights[i];
                light.Tick(dt);

                if (light.IsExpired)
                {
                    light.gameObject.SetActive(false);
                    _activeLights.RemoveAt(i);
                    _lightPool.Enqueue(light);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawn an impact effect using a preset. If preset is null, uses default hull impact.
        /// </summary>
        public void SpawnImpact(Vector2 position, Vector2 normal, ImpactPreset preset = null)
        {
            if (preset == null)
                preset = defaultHullImpact != null ? defaultHullImpact : GetFallbackPreset();

            var systems = EnsureParticleSystems(preset);

            EmitLayer(systems.primary, preset.primaryLayer, position, normal);

            if (preset.enableSecondaryLayer)
                EmitLayer(systems.secondary, preset.secondaryLayer, position, normal);

            if (preset.enableTertiaryLayer)
                EmitLayer(systems.tertiary, preset.tertiaryLayer, position, normal);

            if (preset.enableLight)
                ActivateLight(position, preset.lightConfig);

            if (preset.impactSound != null)
                AudioSource.PlayClipAtPoint(preset.impactSound, position, preset.soundVolume);

            if (preset.screenShakeConfig != null)
                V3CameraShakeService.Instance?.TriggerImpactShake(position, normal, preset.screenShakeConfig);
        }

        /// <summary>
        /// Spawn an impact from a V2ImpactConfig. Uses the preset if assigned, otherwise falls back
        /// to legacy Instantiate/Destroy behavior.
        /// </summary>
        public void SpawnFromConfig(Vector2 position, Vector2 normal, V2ImpactConfig impactConfig)
        {
            if (impactConfig == null) return;

            if (impactConfig.impactPreset != null)
            {
                SpawnImpact(position, normal, impactConfig.impactPreset);
                return;
            }

            // Legacy path: instantiate prefab
            if (impactConfig.impactParticlePrefab != null)
            {
                var particles = Instantiate(
                    impactConfig.impactParticlePrefab,
                    position,
                    Quaternion.LookRotation(Vector3.forward, normal)
                );
                particles.transform.localScale = Vector3.one * impactConfig.particleScale;
                Destroy(particles, impactConfig.effectDuration);
            }

            if (impactConfig.spawnLight)
            {
                ActivateLight(position, new ImpactLightConfig
                {
                    color = impactConfig.lightColor,
                    intensity = impactConfig.lightIntensity,
                    radius = impactConfig.lightRadius,
                    duration = impactConfig.lightDuration
                });
            }

            if (impactConfig.impactSound != null)
                AudioSource.PlayClipAtPoint(impactConfig.impactSound, position, impactConfig.soundVolume);

            if (impactConfig.screenShakeConfig != null)
                V3CameraShakeService.Instance?.TriggerImpactShake(position, normal, impactConfig.screenShakeConfig);
        }

        #endregion

        #region Particle Emission

        private void EmitLayer(ParticleSystem ps, ImpactParticleLayer layer, Vector2 position, Vector2 normal)
        {
            if (ps == null || layer == null) return;

            float halfSpread = layer.spreadAngle * 0.5f * Mathf.Deg2Rad;

            for (int i = 0; i < layer.particleCount; i++)
            {
                float angle = Mathf.Lerp(
                    Random.Range(-Mathf.PI, Mathf.PI),
                    Random.Range(-halfSpread, halfSpread),
                    layer.directionBias
                );

                Vector2 dir = RotateVector(normal, angle);
                float speed = Random.Range(layer.speedRange.x, layer.speedRange.y);

                var emitParams = new ParticleSystem.EmitParams
                {
                    position = new Vector3(position.x, position.y, 0f),
                    velocity = new Vector3(dir.x, dir.y, 0f) * speed,
                    startSize = Random.Range(layer.startSizeRange.x, layer.startSizeRange.y),
                    startLifetime = layer.lifetime * Random.Range(0.8f, 1.2f),
                    startColor = layer.colorOverLifetime.Evaluate(0f)
                };

                ps.Emit(emitParams, 1);
            }
        }

        private static Vector2 RotateVector(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        #endregion

        #region Particle System Creation

        private ParticleSystemSet EnsureParticleSystems(ImpactPreset preset)
        {
            if (_particleSystems.TryGetValue(preset, out var existing))
                return existing;

            var set = new ParticleSystemSet
            {
                primary = CreateParticleSystem($"{preset.name}_Primary", preset.primaryLayer),
                secondary = preset.enableSecondaryLayer
                    ? CreateParticleSystem($"{preset.name}_Secondary", preset.secondaryLayer)
                    : null,
                tertiary = preset.enableTertiaryLayer
                    ? CreateParticleSystem($"{preset.name}_Tertiary", preset.tertiaryLayer)
                    : null
            };

            _particleSystems[preset] = set;
            return set;
        }

        private ParticleSystem CreateParticleSystem(string name, ImpactParticleLayer layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_particleContainer);

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = layer.lifetime;
            main.startSize = new ParticleSystem.MinMaxCurve(layer.startSizeRange.x, layer.startSizeRange.y);
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = config != null ? config.maxParticlesPerSystem : 1000;

            var emission = ps.emission;
            emission.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(layer.colorOverLifetime);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, layer.sizeOverLifetime);

            if (layer.enableCollision)
            {
                var collision = ps.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision2D;
                collision.quality = layer.collisionQuality;
                collision.dampen = layer.bounceDamping;
                collision.bounce = 1f - layer.bounceDamping;
                collision.lifetimeLoss = 0.1f;
                if (config != null) collision.collidesWith = config.collisionLayers;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = layer.renderMode;
            renderer.sortingOrder = layer.sortingOrder;

            if (config != null && config.defaultParticleMaterial != null)
            {
                if (layer.particleSprite != null)
                {
                    renderer.material = new Material(config.defaultParticleMaterial);
                    renderer.material.mainTexture = layer.particleSprite.texture;
                }
                else
                {
                    renderer.material = config.defaultParticleMaterial;
                }
            }

            ps.Play();
            return ps;
        }

        #endregion

        #region Light Pool

        private void ActivateLight(Vector2 position, ImpactLightConfig lightConfig)
        {
            if (lightConfig == null) return;

            ImpactLight impactLight;
            if (_lightPool.Count > 0)
            {
                impactLight = _lightPool.Dequeue();
            }
            else
            {
                impactLight = CreatePooledLight();
            }

            impactLight.Activate(position, lightConfig);
            _activeLights.Add(impactLight);
        }

        private ImpactLight CreatePooledLight()
        {
            var go = new GameObject("ImpactLight_Pooled");
            go.transform.SetParent(_lightContainer);

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;

            var impactLight = go.AddComponent<ImpactLight>();
            impactLight.light2D = light;

            return impactLight;
        }

        #endregion

        #region Fallback Preset

        private ImpactPreset GetFallbackPreset()
        {
            if (_fallbackPreset != null) return _fallbackPreset;

            _fallbackPreset = ScriptableObject.CreateInstance<ImpactPreset>();
            _fallbackPreset.name = "FallbackImpact";
            _fallbackPreset.primaryLayer = new ImpactParticleLayer
            {
                particleCount = 10,
                spreadAngle = 90f,
                directionBias = 0.3f,
                speedRange = new Vector2(2f, 6f),
                lifetime = 0.5f,
                startSizeRange = new Vector2(0.1f, 0.2f)
            };
            _fallbackPreset.enableSecondaryLayer = false;
            _fallbackPreset.enableTertiaryLayer = false;
            _fallbackPreset.enableLight = true;
            _fallbackPreset.lightConfig = new ImpactLightConfig
            {
                color = Color.white,
                intensity = 1f,
                radius = 2f,
                duration = 0.15f
            };

            return _fallbackPreset;
        }

        #endregion

        private class ParticleSystemSet
        {
            public ParticleSystem primary;
            public ParticleSystem secondary;
            public ParticleSystem tertiary;
        }
    }
}
