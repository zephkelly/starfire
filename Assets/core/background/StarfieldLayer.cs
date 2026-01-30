using UnityEngine;
using Starfire.Core;
using Starfire.Core.V2.World;

namespace Starfire.Core.Background
{
    /// <summary>
    /// Base class for all starfield layer types.
    /// Each layer renders to its own quad with its own material.
    /// </summary>
    [System.Serializable]
    public abstract class StarfieldLayer
    {
        [Header("Layer Settings")]
        [Tooltip("Enable/disable this layer")]
        public bool enabled = true;

        [Tooltip("Layer name for identification")]
        public string layerName = "Layer";

        [Tooltip("Parallax depth - lower values = farther/slower, higher = closer/faster")]
        [HighPrecision(5, 0.00001f)]
        public float parallaxDepth = 0.02f;

        // Runtime references (not serialized)
        [System.NonSerialized] protected Material _material;
        [System.NonSerialized] protected GameObject _quadObject;
        [System.NonSerialized] protected MeshRenderer _renderer;

        // Double-precision virtual position set by StarfieldManager each frame
        [System.NonSerialized] protected Vector2D _virtualPos;

        // Fmod period for parallax-space wrapping (keeps float values precise)
        protected const double ParallaxFmodPeriod = 100000.0;

        // Shader property ID for per-material parallax offset
        protected static readonly int ParallaxOffsetID = Shader.PropertyToID("_ParallaxOffset");

        /// <summary>
        /// The shader to use for this layer type.
        /// </summary>
        public abstract Shader GetShader();

        /// <summary>
        /// Apply layer-specific properties to the material.
        /// </summary>
        public abstract void ConfigureMaterial(Material material);

        /// <summary>
        /// Set the double-precision virtual position for this frame.
        /// Called by StarfieldManager before Update().
        /// </summary>
        public void SetVirtualPosition(Vector2D virtualPos)
        {
            _virtualPos = virtualPos;
        }

        /// <summary>
        /// Compute a float-safe parallax offset from the double-precision virtual position.
        /// Uses fmod in parallax-UV space to keep values within float32 precision (~0.01 units).
        /// Seam distance = ParallaxFmodPeriod / parallaxFactor (e.g., 4000km+ for parallax 0.05).
        /// </summary>
        protected Vector2 ComputeParallaxOffset(double parallaxFactor)
        {
            double offsetX = _virtualPos.X * parallaxFactor;
            double offsetY = _virtualPos.Y * parallaxFactor;

            offsetX = SymmetricFmod(offsetX, ParallaxFmodPeriod);
            offsetY = SymmetricFmod(offsetY, ParallaxFmodPeriod);

            return new Vector2((float)offsetX, (float)offsetY);
        }

        /// <summary>
        /// Symmetric fmod: keeps value in [-period/2, +period/2) range.
        /// Computed in double precision to avoid float truncation.
        /// </summary>
        protected static double SymmetricFmod(double value, double period)
        {
            double halfPeriod = period * 0.5;
            value = value % period;
            if (value < -halfPeriod) value += period;
            else if (value >= halfPeriod) value -= period;
            return value;
        }

        /// <summary>
        /// Apply the per-material parallax offset to the material.
        /// Call this in ConfigureMaterial() instead of relying on shader-side _CameraWorldPos * _ParallaxFactor.
        /// </summary>
        protected void ApplyParallaxOffset(Material material)
        {
            Vector2 offset = ComputeParallaxOffset(parallaxDepth);
            material.SetVector(ParallaxOffsetID, new Vector4(offset.x, offset.y, 0, 0));
        }

        /// <summary>
        /// Apply world fabric properties to the material based on this layer's parallax depth.
        /// Call at the end of ConfigureMaterial() to set per-layer fabric values.
        /// </summary>
        protected void ApplyFabricProperties(Material material)
        {
            var bridge = WorldFabricBridge.Instance;
            if (bridge == null) return;

            bridge.ApplyFabricToMaterial(material, parallaxDepth);
        }

        /// <summary>
        /// Initialize the layer's quad and material.
        /// </summary>
        public virtual void Initialize(Transform parent, Mesh quadMesh, int sortOrder)
        {
            if (_quadObject != null) return;

            var shader = GetShader();
            if (shader == null)
            {
                Debug.LogError($"StarfieldLayer '{layerName}': Shader not found!");
                return;
            }

            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;

            _quadObject = new GameObject($"StarfieldLayer_{layerName}");
            _quadObject.hideFlags = HideFlags.DontSave;
            _quadObject.transform.SetParent(parent);

            var meshFilter = _quadObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = quadMesh;

            _renderer = _quadObject.AddComponent<MeshRenderer>();
            _renderer.material = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            // Ensure background renders behind all sprites
            // Use negative sorting order to stay behind default sprites (which start at 0)
            _renderer.sortingLayerName = "Default";
            _renderer.sortingOrder = -1000 + sortOrder;

            ConfigureMaterial(_material);
        }

        /// <summary>
        /// Update the layer each frame.
        /// </summary>
        public virtual void Update()
        {
            if (_material == null) return;
            ConfigureMaterial(_material);
        }

        /// <summary>
        /// Update the quad's transform to match camera view.
        /// </summary>
        public virtual void UpdateTransform(Vector3 cameraPos, float width, float height, float baseDepth, int layerIndex)
        {
            if (_quadObject == null) return;

            float zOffset = baseDepth + (layerIndex * 0.01f);
            _quadObject.transform.position = new Vector3(cameraPos.x, cameraPos.y, cameraPos.z + zOffset);
            _quadObject.transform.localScale = new Vector3(width, height, 1f);
            _quadObject.transform.rotation = Quaternion.identity;
            _quadObject.SetActive(enabled);
        }

        /// <summary>
        /// Cleanup the layer's resources.
        /// </summary>
        public virtual void Cleanup()
        {
            if (_quadObject != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_quadObject);
                    Object.Destroy(_material);
                }
                else
                {
                    Object.DestroyImmediate(_quadObject);
                    Object.DestroyImmediate(_material);
                }
                _quadObject = null;
                _material = null;
                _renderer = null;
            }
        }

        /// <summary>
        /// Check if the layer is properly initialized.
        /// </summary>
        public bool IsInitialized => _quadObject != null && _material != null;
    }
}
