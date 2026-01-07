using UnityEngine;

namespace Starfire.Core.Background
{
    /// <summary>
    /// Creates and manages a full-screen quad for the starfield background.
    /// Attach this to your main camera or an empty GameObject in the scene.
    /// Only creates the quad during play mode.
    /// </summary>
    public class StarfieldBackground : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Material starfieldMaterial;

        [Header("Settings")]
        [Tooltip("Z position of the background quad (should be far behind other objects)")]
        [SerializeField] private float backgroundDepth = 100f;

        [Tooltip("Extra scale multiplier for the quad size")]
        [SerializeField] private float scaleMultiplier = 1.1f;

        private GameObject _quadObject;
        private MeshRenderer _meshRenderer;
        private Camera _camera;

        private static readonly int CameraWorldPosID = Shader.PropertyToID("_CameraWorldPos");
        private static readonly int ScreenAspectID = Shader.PropertyToID("_ScreenAspect");

        private void Awake()
        {
            CleanupStaleQuads();
        }

        private void OnEnable()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                _camera = FindFirstObjectByType<Camera>();
            }

            CreateQuad();
        }

        private void CleanupStaleQuads()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "StarfieldQuad")
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void OnDisable()
        {
            DestroyQuad();
        }

        private void LateUpdate()
        {
            if (_quadObject != null && _camera != null)
            {
                UpdateQuadTransform();
                UpdateShaderProperties();
            }
        }

        private void UpdateShaderProperties()
        {
            if (_camera == null) return;

            Vector3 camPos = _camera.transform.position;
            Shader.SetGlobalVector(CameraWorldPosID, new Vector4(camPos.x, camPos.y, 0, 0));
            Shader.SetGlobalFloat(ScreenAspectID, _camera.aspect);
        }

        private void CreateQuad()
        {
            if (_quadObject != null) return;

            _quadObject = new GameObject("StarfieldQuad");
            _quadObject.hideFlags = HideFlags.DontSave;
            _quadObject.transform.SetParent(transform);

            // Create mesh filter with quad
            var meshFilter = _quadObject.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateQuadMesh();

            // Add mesh renderer with material
            _meshRenderer = _quadObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = starfieldMaterial;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            UpdateQuadTransform();
        }

        private void DestroyQuad()
        {
            if (_quadObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_quadObject);
                }
                else
                {
                    DestroyImmediate(_quadObject);
                }
                _quadObject = null;
            }
        }

        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            mesh.name = "StarfieldQuadMesh";

            // Vertices (centered, unit size)
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };

            // UVs
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            // Triangles
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void UpdateQuadTransform()
        {
            if (_camera == null || _quadObject == null) return;

            // Position quad in front of camera at background depth
            Vector3 cameraPos = _camera.transform.position;
            _quadObject.transform.position = new Vector3(cameraPos.x, cameraPos.y, cameraPos.z + backgroundDepth);

            // Scale to cover camera view
            float height, width;

            if (_camera.orthographic)
            {
                height = _camera.orthographicSize * 2f;
                width = height * _camera.aspect;
            }
            else
            {
                // For perspective camera (if ever needed)
                height = 2f * backgroundDepth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                width = height * _camera.aspect;
            }

            _quadObject.transform.localScale = new Vector3(width * scaleMultiplier, height * scaleMultiplier, 1f);

            // Face camera
            _quadObject.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Call this to refresh the quad if camera settings change.
        /// </summary>
        public void RefreshQuad()
        {
            DestroyQuad();
            CreateQuad();
        }

        private void OnValidate()
        {
            if (_quadObject != null && _camera != null)
            {
                UpdateQuadTransform();
            }
        }
    }
}
