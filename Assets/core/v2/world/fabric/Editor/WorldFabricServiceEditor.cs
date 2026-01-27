using UnityEngine;
using UnityEditor;

namespace StarfireV2.Editor
{
    [CustomEditor(typeof(WorldFabricService))]
    public class WorldFabricServiceEditor : UnityEditor.Editor
    {
        private SerializedProperty _config;
        private SerializedProperty _enablePreview;
        private SerializedProperty _previewMode;
        private SerializedProperty _previewResolution;
        private SerializedProperty _previewWorldSize;
        private SerializedProperty _previewCenter;
        private SerializedProperty _previewFollowCamera;
        private SerializedProperty _previewUpdateInterval;
        private SerializedProperty _previewTexture;

        private bool _showPreviewFoldout = true;
        private float _previewSize = 300f;

        private void OnEnable()
        {
            _config = serializedObject.FindProperty("config");
            _enablePreview = serializedObject.FindProperty("enablePreview");
            _previewMode = serializedObject.FindProperty("previewMode");
            _previewResolution = serializedObject.FindProperty("previewResolution");
            _previewWorldSize = serializedObject.FindProperty("previewWorldSize");
            _previewCenter = serializedObject.FindProperty("previewCenter");
            _previewFollowCamera = serializedObject.FindProperty("previewFollowCamera");
            _previewUpdateInterval = serializedObject.FindProperty("previewUpdateInterval");
            _previewTexture = serializedObject.FindProperty("previewTexture");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var service = (WorldFabricService)target;

            // Config
            EditorGUILayout.PropertyField(_config);
            EditorGUILayout.Space();

            // Preview section
            _showPreviewFoldout = EditorGUILayout.Foldout(_showPreviewFoldout, "World Preview", true, EditorStyles.foldoutHeader);

            if (_showPreviewFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_enablePreview, new GUIContent("Enable Preview"));

                if (_enablePreview.boolValue)
                {
                    EditorGUILayout.PropertyField(_previewMode, new GUIContent("Mode"));
                    EditorGUILayout.PropertyField(_previewResolution, new GUIContent("Resolution"));
                    EditorGUILayout.PropertyField(_previewWorldSize, new GUIContent("World Size"));
                    EditorGUILayout.PropertyField(_previewFollowCamera, new GUIContent("Follow Camera"));

                    if (_previewFollowCamera.boolValue)
                    {
                        EditorGUILayout.PropertyField(_previewUpdateInterval, new GUIContent("Update Interval (s)"));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(_previewCenter, new GUIContent("Center"));
                    }

                    EditorGUILayout.Space();

                    // Generate button
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Generate Preview", GUILayout.Height(25)))
                    {
                        // In edit mode, ensure queries are created from current config
                        if (!Application.isPlaying)
                        {
                            service.InvalidateEditModeQueries();
                            service.EnsureEditModeQueries();
                        }
                        service.GeneratePreview();
                    }
                    if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(25)))
                    {
                        if (!Application.isPlaying)
                        {
                            service.InvalidateEditModeQueries();
                        }
                        service.MarkPreviewDirty();
                    }
                    EditorGUILayout.EndHorizontal();

                    // Edit mode info
                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("Edit Mode: Preview uses config values directly. Changes to SpaceZoneConfig, ResourceConfig, or FactionConfig require clicking 'Generate Preview' to update.", MessageType.Info);
                    }

                    EditorGUILayout.Space();

                    // Preview size slider
                    _previewSize = EditorGUILayout.Slider("Preview Size", _previewSize, 100f, 500f);

                    EditorGUILayout.Space();

                    // Draw the preview texture
                    if (service.PreviewTexture != null)
                    {
                        var rect = GUILayoutUtility.GetRect(_previewSize, _previewSize, GUILayout.ExpandWidth(false));

                        // Center the preview
                        rect.x = (EditorGUIUtility.currentViewWidth - _previewSize) * 0.5f;

                        // Draw background
                        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

                        // Draw texture
                        GUI.DrawTexture(rect, service.PreviewTexture, ScaleMode.ScaleToFit);

                        // Draw border
                        Handles.color = Color.white;
                        Handles.DrawWireDisc(rect.center, Vector3.forward, 2f);

                        // Draw info overlay
                        var infoRect = new Rect(rect.x, rect.yMax + 5, rect.width, 60);
                        EditorGUI.HelpBox(infoRect,
                            $"Center: ({_previewCenter.vector2Value.x:F0}, {_previewCenter.vector2Value.y:F0})\n" +
                            $"World Size: {_previewWorldSize.floatValue:F0} units\n" +
                            $"Resolution: {_previewResolution.intValue}x{_previewResolution.intValue}",
                            MessageType.None);

                        GUILayout.Space(70);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Click 'Generate Preview' to create the world preview.", MessageType.Info);
                    }

                    EditorGUILayout.Space();

                    // Legend
                    DrawLegend();
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLegend()
        {
            EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);

            var mode = (WorldFabricService.PreviewMode)_previewMode.enumValueIndex;

            if (mode == WorldFabricService.PreviewMode.SpaceZones || mode == WorldFabricService.PreviewMode.Combined)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Zone Types:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Deep Void", new Color(0.05f, 0.05f, 0.1f));
                DrawLegendItem("Sparse Space", new Color(0.1f, 0.1f, 0.2f));
                DrawLegendItem("Open Space", new Color(0.2f, 0.2f, 0.3f));
                DrawLegendItem("Asteroid Belt", new Color(0.4f, 0.3f, 0.2f));
                DrawLegendItem("Nebula Dense", new Color(0.5f, 0.2f, 0.5f));
                DrawLegendItem("Anomaly", new Color(0.8f, 0.1f, 0.1f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.Factions || mode == WorldFabricService.PreviewMode.Combined)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Factions:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Neutral", new Color(0.2f, 0.2f, 0.2f));
                DrawLegendItem("Minor Factions", Color.HSVToRGB(0.5f, 0.4f, 0.5f));
                DrawLegendItem("Major Factions", Color.HSVToRGB(0.5f, 0.7f, 0.8f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.FactionBorders || mode == WorldFabricService.PreviewMode.Combined)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Borders:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Contested Zone", Color.red);
                DrawLegendItem("Border Area", Color.yellow);
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.Danger)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Danger Level:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Safe", Color.green);
                DrawLegendItem("Dangerous", Color.red);
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.NebulaDensity)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Nebula Density:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Dense", new Color(0.6f, 0.2f, 0.6f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.AsteroidDensity)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Asteroid Density:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Dense", new Color(0.6f, 0.4f, 0.2f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.VoidFactor)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Void Factor:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Active Space", Color.black);
                DrawLegendItem("Deep Void", new Color(0.1f, 0.1f, 0.3f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.AnomalyStrength)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Anomaly Strength:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Normal", Color.black);
                DrawLegendItem("Anomalous", new Color(0.8f, 0.2f, 0.2f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.MineralDensity)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Mineral Density:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Dense", new Color(0.7f, 0.7f, 0.4f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.OreDensity)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Ore Density:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Dense", new Color(0.6f, 0.3f, 0.1f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.GasDensity)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Gas Density:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Dense", new Color(0.3f, 0.7f, 0.3f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.ExoticDensity)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Exotic Density:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Dense", new Color(0.8f, 0.2f, 0.8f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.WaterDensity)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Water Density:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Dense", new Color(0.2f, 0.5f, 0.9f));
                EditorGUILayout.EndVertical();
            }

            if (mode == WorldFabricService.PreviewMode.ResourceValue)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Resource Value:", EditorStyles.miniBoldLabel);
                DrawLegendItem("None", Color.black);
                DrawLegendItem("Common", new Color(0.1f, 0.3f, 0.05f));
                DrawLegendItem("Uncommon", new Color(0.2f, 0.6f, 0.1f));
                DrawLegendItem("Rare", new Color(0.55f, 0.65f, 0.1f));
                DrawLegendItem("Exotic", new Color(0.9f, 0.7f, 0.1f));
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLegendItem(string label, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            var rect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}
