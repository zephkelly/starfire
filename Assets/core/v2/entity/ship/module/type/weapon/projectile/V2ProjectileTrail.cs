using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Shader-based projectile trail component.
    /// Creates a velocity-driven trail using a custom shader.
    /// </summary>
    public class V2ProjectileTrail : MonoBehaviour
    {
        private static Shader _cachedShader;
        private static Mesh _cachedQuadMesh;

        private V2TrailConfig _config;
        private Rigidbody2D _rigidbody;
        private MeshRenderer _meshRenderer;
        private Transform _trailTransform;
        private Material _material;
        private float _baseWidth;
        private float _baseLength;

        // For non-physics projectiles (raycast mode)
        private Vector3 _previousPosition;
        private bool _hasPreviousPosition;

        // Trail growth tracking
        private float _age;

        // Shader property IDs for performance
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int FalloffPowerId = Shader.PropertyToID("_FalloffPower");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int TrailLengthId = Shader.PropertyToID("_TrailLength");

        /// <summary>
        /// Initialize the trail with configuration.
        /// </summary>
        public void Initialize(
            V2TrailConfig config,
            Color projectileColor,
            float projectileScale,
            SpriteRenderer projectileSprite)
        {
            _config = config;
            _rigidbody = GetComponent<Rigidbody2D>();
            _hasPreviousPosition = false;
            _age = 0f;

            // Calculate base width
            if (config.width > 0f)
            {
                _baseWidth = config.width * projectileScale;
            }
            else if (projectileSprite != null && projectileSprite.sprite != null)
            {
                // Auto-detect from sprite bounds
                var bounds = projectileSprite.sprite.bounds;
                _baseWidth = bounds.size.x * projectileScale * config.widthMultiplier;
            }
            else
            {
                _baseWidth = 0.2f * projectileScale; // Default fallback
            }

            _baseLength = config.length;

            // Create the trail visual
            CreateTrailVisual(config, projectileColor, projectileSprite);
        }

        /// <summary>
        /// Reinitialize the trail for pooled projectile reuse.
        /// Resets state without recreating the visual.
        /// </summary>
        public void Reinitialize(
            V2TrailConfig config,
            Color projectileColor,
            float projectileScale,
            SpriteRenderer projectileSprite)
        {
            _config = config;
            _rigidbody = GetComponent<Rigidbody2D>();
            _hasPreviousPosition = false;
            _age = 0f;

            // Recalculate base width
            if (config.width > 0f)
            {
                _baseWidth = config.width * projectileScale;
            }
            else if (projectileSprite != null && projectileSprite.sprite != null)
            {
                var bounds = projectileSprite.sprite.bounds;
                _baseWidth = bounds.size.x * projectileScale * config.widthMultiplier;
            }
            else
            {
                _baseWidth = 0.2f * projectileScale;
            }

            _baseLength = config.length;

            // Update material properties and reset trail length to zero
            if (_material != null)
            {
                Color trailColor = config.useProjectileColor ? projectileColor : config.color;
                _material.SetColor(ColorId, trailColor);
                _material.SetFloat(GlowIntensityId, config.glowIntensity);
                _material.SetFloat(FalloffPowerId, config.falloffPower);
                _material.SetFloat(SoftnessId, config.softness);
                _material.SetFloat(TrailLengthId, 0f); // Start at zero for growth animation
            }

            // Update fixed width scale if changed
            if (_trailTransform != null)
            {
                _trailTransform.localScale = new Vector3(_baseWidth, 1f, 1f);
            }
        }

        private void CreateTrailVisual(V2TrailConfig config, Color projectileColor, SpriteRenderer projectileSprite)
        {
            // Create child GameObject for the trail
            var trailGO = new GameObject("Trail");
            trailGO.transform.SetParent(transform, false);
            trailGO.transform.localPosition = Vector3.zero;
            _trailTransform = trailGO.transform;

            // Add mesh components
            var meshFilter = trailGO.AddComponent<MeshFilter>();
            _meshRenderer = trailGO.AddComponent<MeshRenderer>();

            // Use cached or create quad mesh
            meshFilter.sharedMesh = GetOrCreateQuadMesh();

            // Create material
            _material = CreateMaterial(config, projectileColor);
            _meshRenderer.material = _material;

            // Set sorting layer to match projectile but behind it
            if (projectileSprite != null)
            {
                _meshRenderer.sortingLayerID = projectileSprite.sortingLayerID;
                _meshRenderer.sortingOrder = projectileSprite.sortingOrder + config.sortingOrderOffset;
            }

            // Initial rotation (180° to point backward from projectile's direction)
            _trailTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);

            // Fixed scale (width only, length handled by shader via _TrailLength uniform)
            _trailTransform.localScale = new Vector3(_baseWidth, 1f, 1f);

            // Start with zero length (shader will stretch to this)
            _material.SetFloat(TrailLengthId, 0f);
        }

        private Material CreateMaterial(V2TrailConfig config, Color projectileColor)
        {
            if (config.customMaterial != null)
            {
                // Use custom material instance
                var mat = new Material(config.customMaterial);
                return mat;
            }

            // Create material from trail shader
            var shader = GetOrCreateShader();
            var material = new Material(shader);

            // Set initial properties
            Color trailColor = config.useProjectileColor ? projectileColor : config.color;
            material.SetColor(ColorId, trailColor);
            material.SetFloat(GlowIntensityId, config.glowIntensity);
            material.SetFloat(FalloffPowerId, config.falloffPower);
            material.SetFloat(SoftnessId, config.softness);
            material.SetFloat(TrailLengthId, config.length);

            return material;
        }

        private static Shader GetOrCreateShader()
        {
            if (_cachedShader == null)
            {
                _cachedShader = Shader.Find("Starfire/ProjectileTrail");

                if (_cachedShader == null)
                {
                    Debug.LogWarning("[V2ProjectileTrail] Could not find Starfire/ProjectileTrail shader, using fallback.");
                    _cachedShader = Shader.Find("Sprites/Default");
                }
            }

            return _cachedShader;
        }

        private static Mesh GetOrCreateQuadMesh()
        {
            if (_cachedQuadMesh != null)
            {
                return _cachedQuadMesh;
            }

            // Create a quad mesh oriented so that:
            // - Y+ points backward (tail direction)
            // - Origin is at the head (projectile position)
            // - UV.y = 0 at head, UV.y = 1 at tail
            var mesh = new Mesh();
            mesh.name = "ProjectileTrailQuad";

            // Vertices: quad from (-.5, 0) to (.5, 1)
            // This puts the head at the bottom (y=0) and tail at top (y=1)
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, 0f),  // Bottom-left (head)
                new Vector3(0.5f, 0f, 0f),   // Bottom-right (head)
                new Vector3(-0.5f, 1f, 0f),  // Top-left (tail)
                new Vector3(0.5f, 1f, 0f)    // Top-right (tail)
            };

            // UVs: match vertex positions for correct shader mapping
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),   // Head left
                new Vector2(1f, 0f),   // Head right
                new Vector2(0f, 1f),   // Tail left
                new Vector2(1f, 1f)    // Tail right
            };

            // Triangles
            mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };

            // Normals (face camera)
            mesh.normals = new Vector3[]
            {
                Vector3.back,
                Vector3.back,
                Vector3.back,
                Vector3.back
            };

            mesh.RecalculateBounds();
            _cachedQuadMesh = mesh;

            return mesh;
        }

        private void LateUpdate()
        {
            if (_trailTransform == null)
            {
                return;
            }

            Vector2 velocity;

            // Get velocity from Rigidbody2D or calculate from position delta
            if (_rigidbody != null)
            {
                velocity = _rigidbody.linearVelocity;
            }
            else
            {
                // Fallback: calculate velocity from position change (for raycast projectiles)
                if (!_hasPreviousPosition)
                {
                    _previousPosition = transform.position;
                    _hasPreviousPosition = true;
                    return;
                }

                Vector3 delta = transform.position - _previousPosition;
                velocity = new Vector2(delta.x, delta.y) / Time.deltaTime;
                _previousPosition = transform.position;
            }

            float speed = velocity.magnitude;

            // Hide trail if below minimum velocity threshold
            if (speed < _config.minVelocityThreshold)
            {
                _material.SetFloat(TrailLengthId, 0f);
                return;
            }

            // Track trail age for growth animation
            _age += Time.deltaTime;

            // Calculate growth factor (0 to 1 over growTime)
            float growthFactor = _config.growTime > 0f
                ? Mathf.Clamp01(_age / _config.growTime)
                : 1f;

            // Calculate trail length based on velocity and growth
            float trailLength = speed * _baseLength * 0.05f * growthFactor;

            // Update trail length via shader uniform (avoids transform hierarchy sync)
            // Rotation is set once in CreateTrailVisual (constant 180°)
            _material.SetFloat(TrailLengthId, trailLength);
        }

        private void OnDestroy()
        {
            // Clean up material instance
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
