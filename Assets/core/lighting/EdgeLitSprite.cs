using UnityEngine;

namespace Starfire.Core.Lighting
{
    /// <summary>
    /// Attach this component to a SpriteRenderer to enable edge lighting from EdgeLightSource lights.
    /// This creates an overlay SpriteRenderer that renders edge lighting additively on top of the base sprite.
    /// The base sprite should use the standard URP Sprite-Lit-Default material for normal 2D lighting.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [ExecuteAlways]
    public class EdgeLitSprite : MonoBehaviour
    {
        [Header("Edge Lighting Settings")]
        [Tooltip("Overall brightness multiplier for edge lighting")]
        [SerializeField, Range(0f, 3f)] private float _edgeBrightness = 1f;

        [Tooltip("Tint color applied to edge lighting")]
        [SerializeField] private Color _edgeTint = Color.white;

        [Tooltip("Width of the edge detection in pixels")]
        [SerializeField, Range(0.5f, 3f)] private float _edgeWidth = 1f;

        [Tooltip("Softness of the edge transition")]
        [SerializeField, Range(0f, 1f)] private float _edgeSoftness = 0.1f;

        [Tooltip("Minimum glow on edges facing away from lights")]
        [SerializeField, Range(0f, 0.5f)] private float _minEdgeGlow = 0f;

        [Header("Light Falloff")]
        [Tooltip("Exponent for distance falloff (2 = quadratic, 1 = linear)")]
        [SerializeField, Range(0.5f, 4f)] private float _falloffExponent = 2f;

        [Header("Overlay Material")]
        [Tooltip("Material using the EdgeLightingOverlay shader. If not set, will try to find one.")]
        [SerializeField] private Material _overlayMaterial;

        private SpriteRenderer _baseSpriteRenderer;
        private SpriteRenderer _overlaySpriteRenderer;
        private GameObject _overlayObject;
        private MaterialPropertyBlock _propertyBlock;
        private bool _initialized;

        private const string OverlayObjectName = "EdgeLightingOverlay";

        // Cached shader property IDs
        private static readonly int EdgeBrightnessID = Shader.PropertyToID("_EdgeBrightness");
        private static readonly int EdgeTintID = Shader.PropertyToID("_EdgeTint");
        private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");
        private static readonly int EdgeSoftnessID = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int MinEdgeGlowID = Shader.PropertyToID("_MinEdgeGlow");
        private static readonly int FalloffExponentID = Shader.PropertyToID("_FalloffExponent");
        private static readonly int LightPositionsID = Shader.PropertyToID("_EdgeLightPositions");
        private static readonly int LightColorsID = Shader.PropertyToID("_EdgeLightColors");
        private static readonly int LightCountID = Shader.PropertyToID("_EdgeLightCount");

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            RegisterWithManager();
            SyncOverlay();
        }

        private void RegisterWithManager()
        {
            // Try Instance first
            if (EdgeLightManager.Instance != null)
            {
                EdgeLightManager.Instance.Register(this);
                return;
            }

            // Fallback: find manager in scene (handles script execution order issues)
            var manager = FindAnyObjectByType<EdgeLightManager>();
            if (manager != null)
            {
                manager.Register(this);
            }
        }

        private void OnDisable()
        {
            EdgeLightManager.Instance?.Unregister(this);
            if (_overlayObject != null)
            {
                _overlayObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            CleanupOverlay();
        }

        private void Initialize()
        {
            if (_initialized) return;

            _baseSpriteRenderer = GetComponent<SpriteRenderer>();
            _propertyBlock = new MaterialPropertyBlock();

            SetupOverlay();
            _initialized = true;
        }

        private void SetupOverlay()
        {
            // Find or create overlay object
            Transform existingOverlay = transform.Find(OverlayObjectName);
            if (existingOverlay != null)
            {
                _overlayObject = existingOverlay.gameObject;
                _overlaySpriteRenderer = _overlayObject.GetComponent<SpriteRenderer>();
            }
            else
            {
                _overlayObject = new GameObject(OverlayObjectName);
                _overlayObject.transform.SetParent(transform, false);
                _overlayObject.transform.localPosition = Vector3.zero;
                _overlayObject.transform.localRotation = Quaternion.identity;
                _overlayObject.transform.localScale = Vector3.one;
                _overlayObject.hideFlags = HideFlags.DontSave;

                _overlaySpriteRenderer = _overlayObject.AddComponent<SpriteRenderer>();
            }

            // Try to find overlay material if not set
            if (_overlayMaterial == null)
            {
                _overlayMaterial = FindOverlayMaterial();
            }

            if (_overlaySpriteRenderer != null && _overlayMaterial != null)
            {
                _overlaySpriteRenderer.material = _overlayMaterial;
                _overlaySpriteRenderer.sortingLayerID = _baseSpriteRenderer.sortingLayerID;
                _overlaySpriteRenderer.sortingOrder = _baseSpriteRenderer.sortingOrder + 1;
            }
        }

        private Material FindOverlayMaterial()
        {
            // Try to load from Resources or find in project
            Material mat = Resources.Load<Material>("EdgeLightingOverlayMaterial");
            if (mat != null) return mat;

            // Try to find shader and create runtime material
            Shader overlayShader = Shader.Find("Starfire/EdgeLightingOverlay");
            if (overlayShader != null)
            {
                mat = new Material(overlayShader);
                mat.hideFlags = HideFlags.DontSave;
                return mat;
            }

            Debug.LogWarning("[EdgeLitSprite] Could not find EdgeLightingOverlay shader or material.");
            return null;
        }

        private void CleanupOverlay()
        {
            if (_overlayObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_overlayObject);
                }
                else
                {
                    DestroyImmediate(_overlayObject);
                }
                _overlayObject = null;
                _overlaySpriteRenderer = null;
            }
        }

        private void LateUpdate()
        {
            if (!_initialized) return;
            SyncOverlay();
        }

        private void SyncOverlay()
        {
            if (_baseSpriteRenderer == null || _overlaySpriteRenderer == null) return;

            // Sync sprite
            if (_overlaySpriteRenderer.sprite != _baseSpriteRenderer.sprite)
            {
                _overlaySpriteRenderer.sprite = _baseSpriteRenderer.sprite;
            }

            // Sync flip
            _overlaySpriteRenderer.flipX = _baseSpriteRenderer.flipX;
            _overlaySpriteRenderer.flipY = _baseSpriteRenderer.flipY;

            // Sync sorting
            _overlaySpriteRenderer.sortingLayerID = _baseSpriteRenderer.sortingLayerID;
            _overlaySpriteRenderer.sortingOrder = _baseSpriteRenderer.sortingOrder + 1;

            // Sync enabled state
            _overlayObject.SetActive(_baseSpriteRenderer.enabled && enabled);
        }

        /// <summary>
        /// Called by EdgeLightManager each frame to update light data.
        /// </summary>
        public void UpdateLightData(Vector4[] positions, Vector4[] colors, int count)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_overlaySpriteRenderer == null) return;

            // Get current property block
            _overlaySpriteRenderer.GetPropertyBlock(_propertyBlock);

            // Set per-sprite settings
            _propertyBlock.SetFloat(EdgeBrightnessID, _edgeBrightness);
            _propertyBlock.SetColor(EdgeTintID, _edgeTint);
            _propertyBlock.SetFloat(EdgeWidthID, _edgeWidth);
            _propertyBlock.SetFloat(EdgeSoftnessID, _edgeSoftness);
            _propertyBlock.SetFloat(MinEdgeGlowID, _minEdgeGlow);
            _propertyBlock.SetFloat(FalloffExponentID, _falloffExponent);

            // Set light data from manager
            _propertyBlock.SetVectorArray(LightPositionsID, positions);
            _propertyBlock.SetVectorArray(LightColorsID, colors);
            _propertyBlock.SetInt(LightCountID, count);

            // Apply to overlay renderer
            _overlaySpriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void OnValidate()
        {
            if (_overlaySpriteRenderer != null && _propertyBlock != null)
            {
                _overlaySpriteRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(EdgeBrightnessID, _edgeBrightness);
                _propertyBlock.SetColor(EdgeTintID, _edgeTint);
                _propertyBlock.SetFloat(EdgeWidthID, _edgeWidth);
                _propertyBlock.SetFloat(EdgeSoftnessID, _edgeSoftness);
                _propertyBlock.SetFloat(MinEdgeGlowID, _minEdgeGlow);
                _propertyBlock.SetFloat(FalloffExponentID, _falloffExponent);
                _overlaySpriteRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// Edge brightness multiplier.
        /// </summary>
        public float EdgeBrightness
        {
            get => _edgeBrightness;
            set => _edgeBrightness = Mathf.Clamp(value, 0f, 3f);
        }

        /// <summary>
        /// Tint color for edge lighting.
        /// </summary>
        public Color EdgeTint
        {
            get => _edgeTint;
            set => _edgeTint = value;
        }

        /// <summary>
        /// Edge detection width in pixels.
        /// </summary>
        public float EdgeWidth
        {
            get => _edgeWidth;
            set => _edgeWidth = Mathf.Clamp(value, 0.5f, 3f);
        }

        /// <summary>
        /// Minimum glow on edges facing away from lights.
        /// </summary>
        public float MinEdgeGlow
        {
            get => _minEdgeGlow;
            set => _minEdgeGlow = Mathf.Clamp(value, 0f, 0.5f);
        }

        /// <summary>
        /// Light distance falloff exponent.
        /// </summary>
        public float FalloffExponent
        {
            get => _falloffExponent;
            set => _falloffExponent = Mathf.Clamp(value, 0.5f, 4f);
        }
    }
}
