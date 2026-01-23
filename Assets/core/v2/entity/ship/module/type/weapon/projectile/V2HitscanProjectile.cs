using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Handles hitscan weapon firing with instant hit detection and beam visual.
    /// This is not a pooled object - it's created on fire and destroyed after visual fades.
    /// </summary>
    public class V2HitscanProjectile : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private float _duration;
        private float _elapsed;
        private Color _startColor;

        /// <summary>
        /// Fires a hitscan from origin in direction, performing instant raycast and spawning visual.
        /// </summary>
        public static void Fire(
            Vector2 origin,
            Vector2 direction,
            float range,
            float damage,
            LayerMask hitLayers,
            IEntityController owner,
            V2WeaponDamageConfig damageConfig,
            V2ProjectileConfig projectileConfig)
        {
            // Perform raycast
            var hit = Physics2D.Raycast(origin, direction, range, hitLayers);

            Vector2 endPoint;
            if (hit.collider != null)
            {
                endPoint = hit.point;

                // Skip if hitting owner
                if (owner != null && !hit.transform.IsChildOf(owner.Transform))
                {
                    // Apply damage
                    var receiver = hit.collider.GetComponentInParent<IV2DamageReceiver>();
                    if (receiver != null)
                    {
                        var damageInfo = new V2DamageInfo
                        {
                            BaseDamage = damage,
                            DamageConfig = damageConfig ?? V2WeaponDamageConfig.Default,
                            Source = owner,
                            HitPoint = hit.point,
                            HitDirection = direction
                        };
                        receiver.ReceiveDamage(damageInfo);
                    }

                    // Spawn impact effects
                    SpawnImpactEffects(hit.point, hit.normal, projectileConfig?.impactConfig);
                }
            }
            else
            {
                endPoint = origin + direction * range;
            }

            // Create visual beam
            CreateBeamVisual(origin, endPoint, projectileConfig);
        }

        private static void CreateBeamVisual(Vector2 start, Vector2 end, V2ProjectileConfig config)
        {
            if (config == null) return;

            var beamGO = new GameObject("HitscanBeam");
            beamGO.transform.position = start;

            var hitscan = beamGO.AddComponent<V2HitscanProjectile>();
            hitscan.Initialize(start, end, config);
        }

        private void Initialize(Vector2 start, Vector2 end, V2ProjectileConfig config)
        {
            _duration = config.hitscanDuration;
            _elapsed = 0f;
            _startColor = config.hitscanColor;

            // Create line renderer
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            _lineRenderer.startWidth = config.hitscanWidth;
            _lineRenderer.endWidth = config.hitscanWidth * 0.5f;

            _lineRenderer.startColor = config.hitscanColor;
            _lineRenderer.endColor = config.hitscanColor;

            if (config.hitscanMaterial != null)
            {
                _lineRenderer.material = config.hitscanMaterial;
            }
            else
            {
                // Use default sprite material
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            _lineRenderer.sortingOrder = 100;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            // Fade out
            float alpha = 1f - (_elapsed / _duration);
            Color fadedColor = _startColor;
            fadedColor.a *= alpha;

            _lineRenderer.startColor = fadedColor;
            _lineRenderer.endColor = fadedColor;
        }

        private static void SpawnImpactEffects(Vector2 hitPoint, Vector2 normal, V2ImpactConfig config)
        {
            if (config == null) return;

            // Spawn particle effect
            if (config.impactParticlePrefab != null)
            {
                var particles = Instantiate(
                    config.impactParticlePrefab,
                    hitPoint,
                    Quaternion.LookRotation(Vector3.forward, normal)
                );
                particles.transform.localScale = Vector3.one * config.particleScale;
                Destroy(particles, config.effectDuration);
            }

            // Spawn impact light
            if (config.spawnLight)
            {
                var lightGO = new GameObject("HitscanImpactLight");
                lightGO.transform.position = hitPoint;

                var light = lightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
                light.color = config.lightColor;
                light.intensity = config.lightIntensity;
                light.pointLightOuterRadius = config.lightRadius;

                Destroy(lightGO, config.lightDuration);
            }

            // Play impact sound
            if (config.impactSound != null)
            {
                AudioSource.PlayClipAtPoint(config.impactSound, hitPoint, config.soundVolume);
            }
        }
    }
}
