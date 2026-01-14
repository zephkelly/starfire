using UnityEngine;
using UnityEditor;

namespace Starfire.Entity.Modules.Shield.Editor
{
    /// <summary>
    /// Custom editor for ShieldVisualConfig that provides a live preview
    /// of the shield visual effect in the Inspector.
    /// </summary>
    [CustomEditor(typeof(ShieldVisualConfig))]
    public class ShieldVisualConfigEditor : UnityEditor.Editor
    {
        private PreviewRenderUtility _previewUtility;
        private Mesh _previewMesh;
        private Material _previewMaterial;
        private Shader _shieldShader;
        private float _previewRotation = 0f;
        private bool _showPreview = true;

        // Shader property IDs
        private static readonly int ShieldColorId = Shader.PropertyToID("_ShieldColor");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int HitFlashColorId = Shader.PropertyToID("_HitFlashColor");
        private static readonly int DamagedColorId = Shader.PropertyToID("_DamagedColor");
        private static readonly int EdgeIntensityId = Shader.PropertyToID("_EdgeIntensity");
        private static readonly int EdgePowerId = Shader.PropertyToID("_EdgePower");
        private static readonly int EdgeThicknessId = Shader.PropertyToID("_EdgeThickness");
        private static readonly int EdgeDitherId = Shader.PropertyToID("_EdgeDither");
        private static readonly int PatternTypeId = Shader.PropertyToID("_PatternType");
        private static readonly int PatternScaleId = Shader.PropertyToID("_PatternScale");
        private static readonly int PatternSpeedId = Shader.PropertyToID("_PatternSpeed");
        private static readonly int PatternIntensityId = Shader.PropertyToID("_PatternIntensity");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int CurrentOpacityId = Shader.PropertyToID("_CurrentOpacity");
        private static readonly int IdleOpacityId = Shader.PropertyToID("_IdleOpacity");
        private static readonly int ActiveOpacityId = Shader.PropertyToID("_ActiveOpacity");
        private static readonly int ShieldHealthId = Shader.PropertyToID("_ShieldHealth");
        private static readonly int RippleSpeedId = Shader.PropertyToID("_RippleSpeed");
        private static readonly int RippleWidthId = Shader.PropertyToID("_RippleWidth");
        private static readonly int RippleIntensityId = Shader.PropertyToID("_RippleIntensity");
        private static readonly int RippleDurationId = Shader.PropertyToID("_RippleDuration");
        private static readonly int RippleOpacityId = Shader.PropertyToID("_RippleOpacity");
        private static readonly int RippleSoftnessId = Shader.PropertyToID("_RippleSoftness");
        private static readonly int RipplePerspectiveId = Shader.PropertyToID("_RipplePerspective");
        private static readonly int ImpactVisibilityRadiusId = Shader.PropertyToID("_ImpactVisibilityRadius");
        private static readonly int ImpactVisibilityFalloffId = Shader.PropertyToID("_ImpactVisibilityFalloff");
        private static readonly int ImpactVisibilitySpeedId = Shader.PropertyToID("_ImpactVisibilitySpeed");

        private void OnEnable()
        {
            InitializePreview();
        }

        private void OnDisable()
        {
            CleanupPreview();
        }

        private void InitializePreview()
        {
            _shieldShader = Shader.Find("Starfire/ShieldBarrier");
            if (_shieldShader == null)
            {
                Debug.LogWarning("ShieldBarrier shader not found for preview");
                return;
            }

            _previewUtility = new PreviewRenderUtility();
            _previewUtility.camera.fieldOfView = 30f;
            _previewUtility.camera.nearClipPlane = 0.1f;
            _previewUtility.camera.farClipPlane = 100f;
            _previewUtility.camera.transform.position = new Vector3(0, 0, -5f);
            _previewUtility.camera.transform.LookAt(Vector3.zero);
            _previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            _previewUtility.camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);

            _previewMesh = CreateEllipseMesh(1f, 1f, 64);
            _previewMaterial = new Material(_shieldShader);
        }

        private void CleanupPreview()
        {
            if (_previewUtility != null)
            {
                _previewUtility.Cleanup();
                _previewUtility = null;
            }

            if (_previewMesh != null)
            {
                DestroyImmediate(_previewMesh);
                _previewMesh = null;
            }

            if (_previewMaterial != null)
            {
                DestroyImmediate(_previewMaterial);
                _previewMaterial = null;
            }
        }

        public override void OnInspectorGUI()
        {
            ShieldVisualConfig config = (ShieldVisualConfig)target;

            // Preview toggle
            EditorGUILayout.Space();
            _showPreview = EditorGUILayout.Foldout(_showPreview, "Shield Preview", true, EditorStyles.foldoutHeader);

            if (_showPreview && _previewUtility != null && _previewMaterial != null)
            {
                // Preview area
                Rect previewRect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true));

                if (Event.current.type == EventType.Repaint)
                {
                    UpdateMaterialFromConfig(config);
                    DrawPreview(previewRect);
                }

                // Rotation slider
                EditorGUI.BeginChangeCheck();
                _previewRotation = EditorGUILayout.Slider("Rotate Preview", _previewRotation, 0f, 360f);
                if (EditorGUI.EndChangeCheck())
                {
                    Repaint();
                }

                EditorGUILayout.HelpBox("This preview shows how the shield will appear in-game.", MessageType.Info);
            }
            else if (_shieldShader == null)
            {
                EditorGUILayout.HelpBox("ShieldBarrier shader not found. Cannot show preview.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Draw default inspector
            DrawDefaultInspector();

            // Force continuous repaints for animated preview
            if (_showPreview)
            {
                Repaint();
            }
        }

        private void UpdateMaterialFromConfig(ShieldVisualConfig config)
        {
            if (_previewMaterial == null) return;

            _previewMaterial.SetColor(ShieldColorId, config.shieldColor);
            _previewMaterial.SetColor(EdgeColorId, config.edgeColor);
            _previewMaterial.SetColor(HitFlashColorId, config.hitFlashColor);
            _previewMaterial.SetColor(DamagedColorId, config.damagedColor);
            _previewMaterial.SetFloat(EdgeIntensityId, config.edgeIntensity);
            _previewMaterial.SetFloat(EdgePowerId, config.fresnelPower);
            _previewMaterial.SetFloat(EdgeThicknessId, config.edgeThickness);
            _previewMaterial.SetFloat(EdgeDitherId, config.edgeDither);
            _previewMaterial.SetInt(PatternTypeId, (int)config.patternType);
            _previewMaterial.SetFloat(PatternScaleId, config.patternScale);
            _previewMaterial.SetFloat(PatternSpeedId, config.patternSpeed);
            _previewMaterial.SetFloat(PatternIntensityId, config.patternIntensity);
            _previewMaterial.SetFloat(PulseAmountId, config.pulseAmount);
            _previewMaterial.SetFloat(PulseSpeedId, config.pulseSpeed);
            _previewMaterial.SetFloat(CurrentOpacityId, config.idleOpacity);
            _previewMaterial.SetFloat(IdleOpacityId, config.idleOpacity);
            _previewMaterial.SetFloat(ActiveOpacityId, config.activeOpacity);
            _previewMaterial.SetFloat(ShieldHealthId, 1f);
            _previewMaterial.SetFloat(RippleSpeedId, config.rippleSpeed);
            _previewMaterial.SetFloat(RippleWidthId, config.rippleWidth);
            _previewMaterial.SetFloat(RippleIntensityId, config.rippleIntensity);
            _previewMaterial.SetFloat(RippleDurationId, config.rippleDuration);
            _previewMaterial.SetFloat(RippleOpacityId, config.rippleOpacity);
            _previewMaterial.SetFloat(RippleSoftnessId, config.rippleSoftness);
            _previewMaterial.SetFloat(RipplePerspectiveId, config.ripplePerspective);
            _previewMaterial.SetFloat(ImpactVisibilityRadiusId, config.impactVisibilityRadius);
            _previewMaterial.SetFloat(ImpactVisibilityFalloffId, config.impactVisibilityFalloff);
            _previewMaterial.SetFloat(ImpactVisibilitySpeedId, config.impactVisibilitySpeed);
        }

        private new void DrawPreview(Rect rect)
        {
            if (_previewUtility == null || _previewMesh == null || _previewMaterial == null)
                return;

            _previewUtility.BeginPreview(rect, GUIStyle.none);

            // Rotate the mesh for visual interest
            Quaternion rotation = Quaternion.Euler(0, _previewRotation, 0);
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one * 1.5f);

            _previewUtility.DrawMesh(_previewMesh, matrix, _previewMaterial, 0);
            _previewUtility.camera.Render();

            Texture resultTexture = _previewUtility.EndPreview();
            GUI.DrawTexture(rect, resultTexture, ScaleMode.ScaleToFit);
        }

        private Mesh CreateEllipseMesh(float radiusX, float radiusY, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ShieldPreviewMesh";

            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[segments + 1];
            Vector3[] normals = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];

            // Center vertex
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            normals[0] = Vector3.back;

            // Edge vertices
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radiusX;
                float y = Mathf.Sin(angle) * radiusY;

                vertices[i + 1] = new Vector3(x, y, 0);
                uvs[i + 1] = new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f);
                normals[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0).normalized;
            }

            // Triangles (fan from center)
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Shield Preview");
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (_previewUtility == null || _previewMesh == null || _previewMaterial == null)
            {
                EditorGUI.HelpBox(r, "Preview unavailable", MessageType.Warning);
                return;
            }

            ShieldVisualConfig config = (ShieldVisualConfig)target;
            UpdateMaterialFromConfig(config);

            // Handle mouse drag to rotate
            if (Event.current.type == EventType.MouseDrag && r.Contains(Event.current.mousePosition))
            {
                _previewRotation += Event.current.delta.x;
                Event.current.Use();
                Repaint();
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawPreview(r);
            }
        }
    }
}
