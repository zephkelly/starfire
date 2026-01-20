using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StarfireV2
{
    /// <summary>
    /// Controls visual effects for a single thruster.
    /// Handles particle emission, light intensity, and glow sprite based on thrust level.
    /// </summary>
    public class ThrusterVisual : MonoBehaviour
    {
        [Header("Components (Auto-detected if not set)")]
        [SerializeField] private ParticleSystem thrusterParticles;
        [SerializeField] private Light2D thrusterLight;
        [SerializeField] private SpriteRenderer thrusterGlow;

        private ThrusterVisualConfig _config;
        private float _currentIntensity;
        private float _targetIntensity;

        // Cached particle system values
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.InheritVelocityModule _inheritVelocity;
        private bool _hasParticles;
        private bool _hasLight;
        private bool _hasGlow;

        // Ship reference for velocity inheritance
        private Rigidbody2D _shipRigidbody;

        private void Awake()
        {
            // Auto-detect components if not assigned
            if (thrusterParticles == null)
                thrusterParticles = GetComponentInChildren<ParticleSystem>();

            if (thrusterLight == null)
                thrusterLight = GetComponentInChildren<Light2D>();

            if (thrusterGlow == null)
                thrusterGlow = GetComponent<SpriteRenderer>();

            CacheComponents();
        }

        private void CacheComponents()
        {
            _hasParticles = thrusterParticles != null;
            _hasLight = thrusterLight != null;
            _hasGlow = thrusterGlow != null;

            if (_hasParticles)
            {
                _emission = thrusterParticles.emission;
                _inheritVelocity = thrusterParticles.inheritVelocity;
            }
        }

        /// <summary>
        /// Initialize the visual with configuration settings.
        /// </summary>
        /// <param name="config">Visual configuration</param>
        /// <param name="shipRigidbody">Optional ship rigidbody for velocity-based particle inheritance</param>
        public void Initialize(ThrusterVisualConfig config, Rigidbody2D shipRigidbody = null)
        {
            _config = config;
            _shipRigidbody = shipRigidbody;
            _currentIntensity = 0f;
            _targetIntensity = 0f;

            // Apply initial light settings
            if (_hasLight && config != null)
            {
                thrusterLight.color = config.LightColor;
                thrusterLight.pointLightOuterRadius = config.LightRadius;
                thrusterLight.intensity = 0f;
            }

            // Set initial glow state
            if (_hasGlow)
            {
                var color = thrusterGlow.color;
                color.a = 0f;
                thrusterGlow.color = color;
            }

            // Ensure particles are stopped initially
            if (_hasParticles)
            {
                _emission.rateOverTime = 0f;
            }
        }

        /// <summary>
        /// Set the target intensity based on normalized thrust (0-1).
        /// Visual will smoothly interpolate to this value.
        /// </summary>
        public void SetIntensity(float normalizedThrust)
        {
            _targetIntensity = Mathf.Clamp01(normalizedThrust);
        }

        private void Update()
        {
            if (_config == null) return;

            // Smooth intensity transition
            _currentIntensity = Mathf.Lerp(
                _currentIntensity,
                _targetIntensity,
                Time.deltaTime * _config.ResponseSpeed
            );

            UpdateParticles();
            UpdateLight();
            UpdateGlow();
            UpdateVelocityInheritance();
        }

        private void UpdateParticles()
        {
            if (!_hasParticles) return;

            float emissionRate = _config.GetEmissionRate(_currentIntensity);
            _emission.rateOverTime = emissionRate;

            // Start/stop particles based on intensity
            if (_currentIntensity > 0.01f && !thrusterParticles.isPlaying)
            {
                thrusterParticles.Play();
            }
            else if (_currentIntensity <= 0.01f && thrusterParticles.isPlaying)
            {
                thrusterParticles.Stop();
            }
        }

        private void UpdateLight()
        {
            if (!_hasLight) return;

            thrusterLight.intensity = _config.GetLightIntensity(_currentIntensity);
        }

        private void UpdateGlow()
        {
            if (!_hasGlow) return;

            // Update scale
            float scale = _config.GetGlowScale(_currentIntensity);
            thrusterGlow.transform.localScale = Vector3.one * scale;

            // Update color and alpha
            Color color = _config.GetGlowColor(_currentIntensity);
            color.a = _currentIntensity * _config.MaxGlowAlpha;
            thrusterGlow.color = color;
        }

        private void UpdateVelocityInheritance()
        {
            if (!_hasParticles || _shipRigidbody == null || _config == null) return;

            // Get ship speed and calculate dynamic inheritance
            float shipSpeed = _shipRigidbody.linearVelocity.magnitude;
            float inheritance = _config.GetVelocityInheritance(shipSpeed);

            _inheritVelocity.curveMultiplier = inheritance;
        }

        /// <summary>
        /// Immediately set intensity without smoothing.
        /// Useful for initialization or instant state changes.
        /// </summary>
        public void SetIntensityImmediate(float normalizedThrust)
        {
            _targetIntensity = Mathf.Clamp01(normalizedThrust);
            _currentIntensity = _targetIntensity;

            if (_config != null)
            {
                UpdateParticles();
                UpdateLight();
                UpdateGlow();
            }
        }

        /// <summary>
        /// Get the current visual intensity.
        /// </summary>
        public float GetCurrentIntensity() => _currentIntensity;

        private void OnDisable()
        {
            // Stop particles when disabled
            if (_hasParticles && thrusterParticles.isPlaying)
            {
                thrusterParticles.Stop();
            }
        }

        #region Code Generation

        /// <summary>
        /// Creates a ThrusterVisual with code-generated particle system and light.
        /// No prefab required.
        /// </summary>
        /// <param name="parent">Parent transform for the visual</param>
        /// <param name="config">Visual configuration</param>
        /// <param name="shipRigidbody">Optional ship rigidbody for velocity-based particle inheritance</param>
        public static ThrusterVisual CreateFromCode(Transform parent, ThrusterVisualConfig config, Rigidbody2D shipRigidbody = null)
        {
            var go = new GameObject("ThrusterVisual");
            go.transform.SetParent(parent, false);

            var visual = go.AddComponent<ThrusterVisual>();
            visual.CreateParticleSystem(config);

            if (config.EnableLight)
            {
                visual.CreateLight2D(config);
            }

            visual.CacheComponents();
            visual.Initialize(config, shipRigidbody);

            return visual;
        }

        private void CreateParticleSystem(ThrusterVisualConfig config)
        {
            thrusterParticles = gameObject.AddComponent<ParticleSystem>();

            // Stop auto-play
            thrusterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Main module
            var main = thrusterParticles.main;
            main.startLifetime = config.ParticleLifetime;
            main.startSpeed = config.ParticleSpeed;
            main.startSize = config.ParticleStartSize;
            main.startColor = config.ParticleStartColor;
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // Local space - particles follow ship
            main.maxParticles = 200;
            main.playOnAwake = false;

            // Emission module
            var emission = thrusterParticles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f; // Controlled by SetIntensity()

            // Velocity inheritance - dynamic based on ship speed
            var inheritVelocity = thrusterParticles.inheritVelocity;
            inheritVelocity.enabled = true;
            inheritVelocity.mode = ParticleSystemInheritVelocityMode.Initial;
            inheritVelocity.curveMultiplier = config.VelocityInheritanceLow; // Start value, updated dynamically

            // Shape module - cone pointing in local forward (up in 2D)
            var shape = thrusterParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = config.ParticleConeAngle;
            shape.radius = 0.02f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // Point along local Y axis

            // Color over lifetime - fade out
            var colorOverLifetime = thrusterParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(config.ParticleStartColor, 0f),
                    new GradientColorKey(config.ParticleEndColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(config.ParticleStartColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Size over lifetime - shrink
            var sizeOverLifetime = thrusterParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;

            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, config.ParticleEndSizeMultiplier);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer settings
            var renderer = thrusterParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 10; // Render above ship

            // Use default particle material (or you could load a specific one)
            renderer.material = GetDefaultParticleMaterial();
        }

        private void CreateLight2D(ThrusterVisualConfig config)
        {
            var lightGO = new GameObject("ThrusterLight");
            lightGO.transform.SetParent(transform, false);

            thrusterLight = lightGO.AddComponent<Light2D>();
            thrusterLight.lightType = Light2D.LightType.Point;
            thrusterLight.color = config.LightColor;
            thrusterLight.intensity = 0f;
            thrusterLight.pointLightOuterRadius = config.LightRadius;
            thrusterLight.pointLightInnerRadius = config.LightRadius * 0.3f;
        }

        private static Material GetDefaultParticleMaterial()
        {
            // Try to find a suitable particle material
            var material = Resources.Load<Material>("Materials/ThrusterParticle");
            if (material != null) return material;

            // Fallback: create a simple additive material
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Particles/Additive");

            if (shader != null)
            {
                material = new Material(shader);
                material.SetFloat("_Mode", 1); // Additive
                return material;
            }

            // Last resort: use default sprite material
            return UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
                ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultParticleMaterial
                : null;
        }

        #endregion
    }
}
