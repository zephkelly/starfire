using UnityEngine;
using UnityEditor;
using Starfire.Core.V2.World.Simulation;

namespace Starfire.Core.V2.World.Simulation.Editor
{
    [CustomEditor(typeof(BackgroundSimulationManager))]
    public class BackgroundSimulationManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _config;
        private SerializedProperty _enablePreview;
        private SerializedProperty _previewMode;
        private SerializedProperty _previewResolution;
        private SerializedProperty _previewWorldSize;
        private SerializedProperty _previewFollowPlayer;
        private SerializedProperty _previewCenter;
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
            _previewFollowPlayer = serializedObject.FindProperty("previewFollowPlayer");
            _previewCenter = serializedObject.FindProperty("previewCenter");
            _previewUpdateInterval = serializedObject.FindProperty("previewUpdateInterval");
            _previewTexture = serializedObject.FindProperty("previewTexture");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var manager = (BackgroundSimulationManager)target;

            // Config
            EditorGUILayout.PropertyField(_config);
            EditorGUILayout.Space();

            // Statistics
            if (Application.isPlaying)
            {
                var (tier1Count, tier2Count) = manager.GetEntityCounts();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Simulation Statistics", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Tier 1 (Active):     {tier1Count} entities");
                EditorGUILayout.LabelField($"Tier 2 (Ballistic):  {tier2Count} snapshots");
                EditorGUILayout.LabelField($"Total:               {tier1Count + tier2Count}");
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            // Preview section
            _showPreviewFoldout = EditorGUILayout.Foldout(_showPreviewFoldout, "Simulation Preview", true, EditorStyles.foldoutHeader);

            if (_showPreviewFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_enablePreview, new GUIContent("Enable Preview"));

                if (_enablePreview.boolValue)
                {
                    EditorGUILayout.PropertyField(_previewMode, new GUIContent("Mode"));
                    _previewResolution.intValue = EditorGUILayout.IntSlider("Resolution", _previewResolution.intValue, 64, 512);
                    _previewWorldSize.floatValue = EditorGUILayout.Slider("World Size", _previewWorldSize.floatValue, 1000f, 100000f);
                    EditorGUILayout.PropertyField(_previewFollowPlayer, new GUIContent("Follow Player"));

                    if (_previewFollowPlayer.boolValue)
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
                        manager.GeneratePreview();
                    }
                    if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(25)))
                    {
                        manager.MarkPreviewDirty();
                    }
                    EditorGUILayout.EndHorizontal();

                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("Preview requires Play Mode to show simulation data.", MessageType.Info);
                    }

                    EditorGUILayout.Space();

                    // Preview size slider
                    _previewSize = EditorGUILayout.Slider("Preview Size", _previewSize, 100f, 500f);

                    EditorGUILayout.Space();

                    // Draw the preview texture
                    if (manager.PreviewTexture != null)
                    {
                        var rect = GUILayoutUtility.GetRect(_previewSize, _previewSize, GUILayout.ExpandWidth(false));

                        // Center the preview
                        rect.x = (EditorGUIUtility.currentViewWidth - _previewSize) * 0.5f;

                        // Draw background
                        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

                        // Draw texture
                        GUI.DrawTexture(rect, manager.PreviewTexture, ScaleMode.ScaleToFit);

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
                        EditorGUILayout.HelpBox("Click 'Generate Preview' to create the simulation preview.", MessageType.Info);
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

            var mode = (SimulationPreviewMode)_previewMode.enumValueIndex;

            // Zone colors (shown for all modes except Entities-only)
            if (mode != SimulationPreviewMode.Entities)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Zone Types:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Loaded Chunks", new Color(0.2f, 0.3f, 0.5f));
                DrawLegendItem("Tier 1 (Active Sim)", new Color(0.15f, 0.4f, 0.2f));
                DrawLegendItem("Tier 2 (Ballistic)", new Color(0.4f, 0.35f, 0.15f));
                DrawLegendItem("Beyond Simulation", new Color(0.08f, 0.05f, 0.12f));
                EditorGUILayout.EndVertical();
            }

            // Entity colors (shown for Combined, Entities, and ChunkGrid)
            if (mode == SimulationPreviewMode.Combined ||
                mode == SimulationPreviewMode.Entities ||
                mode == SimulationPreviewMode.ChunkGrid)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Entities:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Tier 1 Entity", new Color(0f, 1f, 1f));
                DrawLegendItem("Tier 2 Entity", new Color(1f, 0.6f, 0f));
                DrawLegendItem("Player Position", Color.white);
                EditorGUILayout.EndVertical();
            }

            if (mode == SimulationPreviewMode.ChunkGrid)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Grid:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Chunk Boundaries", new Color(0.7f, 0.7f, 0.7f));
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
