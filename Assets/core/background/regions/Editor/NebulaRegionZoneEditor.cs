using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Starfire.Core.Background.Regions.Editor
{
    /// <summary>
    /// Custom editor for NebulaRegionZone with scene handles for radius manipulation.
    /// </summary>
    [CustomEditor(typeof(NebulaRegionZone))]
    [CanEditMultipleObjects]
    public class NebulaRegionZoneEditor : UnityEditor.Editor
    {
        private SphereBoundsHandle _radiusHandle;

        private SerializedProperty _configProperty;
        private SerializedProperty _radiusProperty;
        private SerializedProperty _useLocalRadiusProperty;
        private SerializedProperty _gizmoColorProperty;
        private SerializedProperty _showGizmoProperty;

        private void OnEnable()
        {
            _radiusHandle = new SphereBoundsHandle();

            _configProperty = serializedObject.FindProperty("config");
            _radiusProperty = serializedObject.FindProperty("radius");
            _useLocalRadiusProperty = serializedObject.FindProperty("useLocalRadius");
            _gizmoColorProperty = serializedObject.FindProperty("gizmoColor");
            _showGizmoProperty = serializedObject.FindProperty("showGizmo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var zone = (NebulaRegionZone)target;

            // Configuration section
            EditorGUILayout.PropertyField(_configProperty);

            if (zone.Config == null)
            {
                EditorGUILayout.HelpBox("Assign a NebulaRegionConfig to define this region's appearance.", MessageType.Warning);
            }
            else
            {
                // Show config info
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Edge Behavior", zone.Config.edgeBehavior.ToString());
                EditorGUILayout.LabelField("Nebula Type", zone.Config.useNebula ? "Nebula" : "Gas Cloud");
                if (zone.Config.useNebula && zone.Config.nebulaPreset != null)
                {
                    EditorGUILayout.LabelField("Preset", zone.Config.nebulaPreset.name);
                }
                else if (!zone.Config.useNebula && zone.Config.gasCloudPreset != null)
                {
                    EditorGUILayout.LabelField("Preset", zone.Config.gasCloudPreset.name);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Radius section
            EditorGUILayout.PropertyField(_useLocalRadiusProperty, new GUIContent("Override Radius"));

            if (_useLocalRadiusProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_radiusProperty);
                EditorGUI.indentLevel--;
            }
            else if (zone.Config != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.FloatField("Radius (from Config)", zone.Config.defaultRadius);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space();

            // Debug visualization section
            EditorGUILayout.LabelField("Debug Visualization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showGizmoProperty);
            if (_showGizmoProperty.boolValue)
            {
                EditorGUILayout.PropertyField(_gizmoColorProperty);
            }

            EditorGUILayout.Space();

            // Runtime info
            if (Application.isPlaying && zone.RuntimeRegion != null)
            {
                EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Region ID", zone.RuntimeRegion.Id);
                EditorGUILayout.Vector2Field("World Position", zone.RuntimeRegion.WorldPosition);
                EditorGUILayout.FloatField("Active Radius", zone.RuntimeRegion.Radius);
                EditorGUILayout.Toggle("Is Active", zone.RuntimeRegion.IsActive);
                EditorGUI.EndDisabledGroup();
            }

            // Quick actions
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Config Asset"))
            {
                CreateConfigAsset();
            }

            if (zone.Config != null && GUILayout.Button("Select Config"))
            {
                Selection.activeObject = zone.Config;
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var zone = (NebulaRegionZone)target;
            if (zone == null) return;

            // Get current radius
            float currentRadius = zone.Radius;
            Vector3 center = zone.transform.position;

            // Setup handle
            _radiusHandle.center = center;
            _radiusHandle.radius = currentRadius;

            // Determine handle color based on edge behavior
            Color handleColor = Color.cyan;
            if (zone.Config != null)
            {
                handleColor = zone.Config.edgeBehavior switch
                {
                    NebulaEdgeBehavior.SmoothFalloff => new Color(0.2f, 0.6f, 1f, 1f),
                    NebulaEdgeBehavior.SharpBoundary => new Color(1f, 0.4f, 0.2f, 1f),
                    NebulaEdgeBehavior.InverseFalloff => new Color(0.8f, 0.2f, 0.8f, 1f),
                    _ => Color.cyan
                };
            }

            using (new Handles.DrawingScope(handleColor))
            {
                EditorGUI.BeginChangeCheck();

                _radiusHandle.DrawHandle();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Change Nebula Region Radius");
                    zone.Radius = _radiusHandle.radius;
                    EditorUtility.SetDirty(zone);
                }
            }

            // Draw falloff ring for smooth/inverse modes
            if (zone.Config != null)
            {
                if (zone.Config.edgeBehavior == NebulaEdgeBehavior.SmoothFalloff && zone.Config.falloffDistance > 0)
                {
                    float innerRadius = currentRadius - zone.Config.falloffDistance;
                    if (innerRadius > 0)
                    {
                        Handles.color = new Color(handleColor.r, handleColor.g, handleColor.b, 0.3f);
                        Handles.DrawWireDisc(center, Vector3.forward, innerRadius);
                    }
                }
                else if (zone.Config.edgeBehavior == NebulaEdgeBehavior.InverseFalloff && zone.Config.falloffDistance > 0)
                {
                    float outerRadius = currentRadius + zone.Config.falloffDistance;
                    Handles.color = new Color(handleColor.r, handleColor.g, handleColor.b, 0.3f);
                    Handles.DrawWireDisc(center, Vector3.forward, outerRadius);
                }
            }

            // Label
            Handles.Label(center + Vector3.up * (currentRadius + 3f),
                $"{zone.name}\nRadius: {currentRadius:F1}",
                EditorStyles.whiteBoldLabel);
        }

        private void CreateConfigAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Nebula Region Config",
                "NewNebulaRegionConfig",
                "asset",
                "Create a new Nebula Region Config asset"
            );

            if (string.IsNullOrEmpty(path)) return;

            var config = CreateInstance<NebulaRegionConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            _configProperty.objectReferenceValue = config;
            serializedObject.ApplyModifiedProperties();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
