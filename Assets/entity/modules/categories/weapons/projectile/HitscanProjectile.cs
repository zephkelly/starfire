using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Hitscan projectile that performs an instant raycast and applies damage immediately.
    /// Displays a visual beam (LineRenderer) that fades out over time.
    /// Best for laser beams and instant-hit weapons.
    /// </summary>
    public class HitscanProjectile : MonoBehaviour
    {
        private HitscanVisualConfig _visualConfig;
        private LineRenderer _lineRenderer;

        private float _fadeDuration;
        private float _elapsed;
        private Color _startColor;
        private float _startWidth;

        /// <summary>
        /// Initialize the hitscan projectile with spawn context.
        /// Performs raycast and applies damage immediately.
        /// </summary>
        public void Initialize(ProjectileSpawnContext context)
        {
            var config = context.ProjectileConfig;
            _visualConfig = config.hitscanConfig ?? new HitscanVisualConfig();
            _fadeDuration = _visualConfig.fadeDuration;

            // Calculate effective speed and max distance
            float effectiveSpeed = config.speed;
            if (config.inheritVelocity && context.InheritedVelocity.sqrMagnitude > 0.01f)
            {
                effectiveSpeed += Vector2.Dot(context.InheritedVelocity, context.Direction);
            }
            effectiveSpeed = Mathf.Max(effectiveSpeed, 1f);
            float maxDistance = effectiveSpeed * config.lifetime;

            // Calculate velocity for impact calculations
            Vector2 velocity = context.Direction * effectiveSpeed;

            // Perform instant raycast
            bool bypassShields = context.DamageConfig?.bypassesShield ?? false;

            var hitResult = ProjectileRaycastUtility.Raycast(
                context.SpawnPosition,
                context.Direction,
                maxDistance,
                config.hitLayers,
                context.Owner,
                bypassShields
            );

            // Determine end point
            Vector2 endPoint = hitResult.DidHit
                ? hitResult.HitPoint
                : context.SpawnPosition + context.Direction * maxDistance;

            // Apply damage immediately if hit
            if (hitResult.DidHit)
            {
                ProjectileRaycastUtility.ApplyDamage(
                    hitResult,
                    context.Damage,
                    context.DamageConfig,
                    context.Owner,
                    context.Direction,
                    config.impactConfig,
                    velocity
                );
            }

            // Set up visual beam
            SetupLineRenderer(context.SpawnPosition, endPoint);
        }

        private void SetupLineRenderer(Vector2 start, Vector2 end)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();

            // Store initial values for fading
            _startColor = _visualConfig.beamColor;
            _startWidth = _visualConfig.beamWidth;

            // Configure line
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            _lineRenderer.startWidth = _startWidth;
            _lineRenderer.endWidth = _startWidth * _visualConfig.endWidthRatio;

            _lineRenderer.startColor = _startColor;
            _lineRenderer.endColor = _startColor;

            // Material setup
            if (_visualConfig.beamMaterial != null)
            {
                _lineRenderer.material = _visualConfig.beamMaterial;
            }
            else
            {
                // Create additive material for glowing beam effect
                var material = new Material(Shader.Find("Sprites/Default"));
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                _lineRenderer.material = material;
            }

            _lineRenderer.sortingOrder = _visualConfig.sortingOrder;
            _lineRenderer.useWorldSpace = true;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float t = _fadeDuration > 0 ? _elapsed / _fadeDuration : 1f;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // Fade out alpha
            float alpha = 1f - t;
            Color fadedColor = _startColor;
            fadedColor.a *= alpha;

            _lineRenderer.startColor = fadedColor;
            _lineRenderer.endColor = fadedColor;

            // Optionally shrink width
            if (_visualConfig.shrinkOnFade)
            {
                float width = _startWidth * alpha;
                _lineRenderer.startWidth = width;
                _lineRenderer.endWidth = width * _visualConfig.endWidthRatio;
            }
        }
    }
}
