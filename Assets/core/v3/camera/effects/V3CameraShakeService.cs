using StarfireV2;
using UnityEngine;

namespace Starfire.Core.V3.Cam.Effects
{
    /// <summary>
    /// Singleton service for triggering camera shake from anywhere in the codebase.
    /// Handles distance-based attenuation that scales with camera ortho size.
    /// </summary>
    public class V3CameraShakeService : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance. May be null if camera system isn't initialized.
        /// </summary>
        public static V3CameraShakeService Instance { get; private set; }

        private V3CameraEffectsManager _effectsManager;
        private Camera _camera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Initializes the service with required references.
        /// Called by CameraController during setup.
        /// </summary>
        public void Initialize(V3CameraEffectsManager effectsManager, Camera camera)
        {
            _effectsManager = effectsManager;
            _camera = camera;
        }

        /// <summary>
        /// Triggers screen shake from a projectile impact with distance-based attenuation.
        /// </summary>
        /// <param name="impactPosition">World position of the impact.</param>
        /// <param name="impactDirection">Direction of impact (typically hit normal or projectile direction).</param>
        /// <param name="config">Per-projectile shake configuration.</param>
        public void TriggerImpactShake(Vector2 impactPosition, Vector2 impactDirection, V2ScreenShakeConfig config)
        {
            if (config == null || _effectsManager == null || _camera == null)
            {
                return;
            }

            float attenuation = CalculateAttenuation(impactPosition, config);

            // Skip if too far away
            if (attenuation <= 0.001f)
            {
                return;
            }

            // Apply trauma with attenuation
            if (config.traumaAmount > 0f)
            {
                float attenuatedTrauma = config.traumaAmount * attenuation;
                _effectsManager.AddTrauma(attenuatedTrauma);
            }

            // Apply punch with attenuation
            if (config.enablePunch && config.punchForce > 0f)
            {
                // Punch direction: hit normal pushes camera away from impact, projectile dir follows projectile
                Vector2 punchDirection = config.useHitNormal ? -impactDirection : impactDirection;
                float attenuatedForce = config.punchForce * attenuation;
                _effectsManager.Punch(punchDirection, attenuatedForce, config.punchDuration);
            }
        }

        /// <summary>
        /// Triggers screen shake from a weapon firing with distance-based attenuation.
        /// Punch direction is opposite to fire direction (recoil effect).
        /// </summary>
        /// <param name="firePosition">World position of the weapon muzzle.</param>
        /// <param name="fireDirection">Direction the weapon fired (punch will be opposite for recoil).</param>
        /// <param name="config">Per-weapon fire shake configuration.</param>
        public void TriggerFireShake(Vector2 firePosition, Vector2 fireDirection, V2FireShakeConfig config)
        {
            if (config == null || _effectsManager == null || _camera == null)
            {
                return;
            }

            float attenuation = CalculateFireAttenuation(firePosition, config);

            // Skip if too far away
            if (attenuation <= 0.001f)
            {
                return;
            }

            // Apply trauma with attenuation
            if (config.traumaAmount > 0f)
            {
                float attenuatedTrauma = config.traumaAmount * attenuation;
                _effectsManager.AddTrauma(attenuatedTrauma);
            }

            // Apply punch with attenuation (opposite direction for recoil)
            if (config.enablePunch && config.punchForce > 0f)
            {
                Vector2 recoilDirection = -fireDirection.normalized;
                float attenuatedForce = config.punchForce * attenuation;
                _effectsManager.Punch(recoilDirection, attenuatedForce, config.punchDuration);
            }
        }

        /// <summary>
        /// Directly adds trauma to the camera shake without distance attenuation.
        /// Useful for player-local effects like firing weapons.
        /// </summary>
        public void AddTraumaDirectly(float trauma)
        {
            _effectsManager?.AddTrauma(trauma);
        }

        /// <summary>
        /// Directly triggers a punch effect without distance attenuation.
        /// </summary>
        public void PunchDirectly(Vector2 direction, float force, float duration = 0.15f)
        {
            _effectsManager?.Punch(direction, force, duration);
        }

        /// <summary>
        /// Calculates distance-based attenuation factor for impact shake.
        /// </summary>
        private float CalculateAttenuation(Vector2 impactPosition, V2ScreenShakeConfig config)
        {
            Vector2 cameraPos = _camera.transform.position;
            float distance = Vector2.Distance(impactPosition, cameraPos);

            // Calculate effective max distance, optionally scaled by ortho size
            float maxDist = config.maxEffectDistance;
            if (config.scaleWithOrthoSize)
            {
                // At larger ortho sizes (zoomed out), effects from further away should still be felt
                float orthoScale = _camera.orthographicSize / V2ScreenShakeConfig.ReferenceOrthoSize;
                maxDist *= orthoScale;
            }

            // Beyond max distance = no effect
            if (distance >= maxDist)
            {
                return 0f;
            }

            // Normalize distance and evaluate curve
            float normalizedDist = distance / maxDist;
            return config.attenuationCurve.Evaluate(normalizedDist);
        }

        /// <summary>
        /// Calculates distance-based attenuation factor for fire shake.
        /// </summary>
        private float CalculateFireAttenuation(Vector2 firePosition, V2FireShakeConfig config)
        {
            Vector2 cameraPos = _camera.transform.position;
            float distance = Vector2.Distance(firePosition, cameraPos);

            // Calculate effective max distance, optionally scaled by ortho size
            float maxDist = config.maxEffectDistance;
            if (config.scaleWithOrthoSize)
            {
                float orthoScale = _camera.orthographicSize / V2FireShakeConfig.ReferenceOrthoSize;
                maxDist *= orthoScale;
            }

            // Beyond max distance = no effect
            if (distance >= maxDist)
            {
                return 0f;
            }

            // Normalize distance and evaluate curve
            float normalizedDist = distance / maxDist;
            return config.attenuationCurve.Evaluate(normalizedDist);
        }
    }
}
