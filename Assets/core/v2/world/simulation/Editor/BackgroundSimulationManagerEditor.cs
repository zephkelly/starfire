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

        // Behavior System properties
        private SerializedProperty _enableBehaviorSystem;
        private SerializedProperty _entityTypeConfigPath;

        private bool _showPreviewFoldout = true;
        private bool _showBehaviorSystemFoldout = true;
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

            // Behavior System properties
            _enableBehaviorSystem = serializedObject.FindProperty("enableBehaviorSystem");
            _entityTypeConfigPath = serializedObject.FindProperty("entityTypeConfigPath");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var manager = (BackgroundSimulationManager)target;

            // Config
            EditorGUILayout.PropertyField(_config);
            EditorGUILayout.Space();

            // Behavior System section
            _showBehaviorSystemFoldout = EditorGUILayout.Foldout(_showBehaviorSystemFoldout, "Behavior System", true, EditorStyles.foldoutHeader);
            if (_showBehaviorSystemFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_enableBehaviorSystem, new GUIContent("Enable Behavior System"));

                if (_enableBehaviorSystem.boolValue)
                {
                    EditorGUILayout.PropertyField(_entityTypeConfigPath, new GUIContent("Entity Type Config Path"));
                    EditorGUILayout.HelpBox(
                        "Place SimulationEntityTypeConfig assets in:\nResources/" + _entityTypeConfigPath.stringValue,
                        MessageType.Info);
                }

                // Show runtime status
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

                    bool behaviorActive = manager.BehaviorSystemActive;
                    var registry = manager.Registry;

                    EditorGUILayout.LabelField($"Behavior System Active: {(behaviorActive ? "Yes" : "No")}");

                    if (registry != null)
                    {
                        EditorGUILayout.LabelField($"Registered Entity Types: {registry.Count}");

                        if (registry.Count > 0)
                        {
                            EditorGUI.indentLevel++;
                            foreach (var config in registry.AllConfigs)
                            {
                                EditorGUILayout.LabelField($"• {config.DisplayName} ({config.EntityType})", EditorStyles.miniLabel);
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Registry: Not initialized");
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Statistics
            if (Application.isPlaying)
            {
                var (tier1Count, tier2Count) = manager.GetEntityCounts();
                var loadedCount = manager.GetLoadedEntityCount();
                var (asteroids, ships, stations, projectiles, other) = manager.GetEntityCountsByType();
                var (totalEvents, collisions, destructions) = manager.GetEventStats();
                var (lastCollisions, lastDestructions) = manager.GetLastFrameStats();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Entity Tracking", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Loaded (GameObjects): {loadedCount} entities");
                EditorGUILayout.LabelField($"Tier 1 (Active Sim):  {tier1Count} entities");
                EditorGUILayout.LabelField($"Tier 2 (Ballistic):   {tier2Count} snapshots");
                EditorGUILayout.LabelField($"Total Simulated:      {tier1Count + tier2Count}");
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Simulated By Type:", EditorStyles.miniBoldLabel);
                if (asteroids > 0) EditorGUILayout.LabelField($"  Asteroids:          {asteroids}", EditorStyles.miniLabel);
                if (ships > 0) EditorGUILayout.LabelField($"  Ships:              {ships}", EditorStyles.miniLabel);
                if (stations > 0) EditorGUILayout.LabelField($"  Stations:           {stations}", EditorStyles.miniLabel);
                if (projectiles > 0) EditorGUILayout.LabelField($"  Projectiles:        {projectiles}", EditorStyles.miniLabel);
                if (other > 0) EditorGUILayout.LabelField($"  Other:              {other}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Collision System", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Total Events:        {totalEvents}");
                EditorGUILayout.LabelField($"  Collisions:        {collisions}");
                EditorGUILayout.LabelField($"  Destructions:      {destructions}");
                if (lastCollisions > 0 || lastDestructions > 0)
                {
                    EditorGUILayout.LabelField($"Last Frame: {lastCollisions} collisions, {lastDestructions} destructions", EditorStyles.miniLabel);
                }
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
                // Loaded entities note
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Brightness indicates state:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("  Bright = Loaded (GameObject)", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  Normal = Tier 1 (Active Sim)", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  Dim = Tier 2 (Ballistic)", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Asteroids:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Loaded", Color.Lerp(new Color(0.6f, 0.4f, 0.2f), Color.white, 0.3f));
                DrawLegendItem("Tier 1", new Color(0.6f, 0.4f, 0.2f));
                DrawLegendItem("Tier 2", Color.Lerp(new Color(0.6f, 0.4f, 0.2f), Color.black, 0.3f));
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Ships:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Loaded", Color.Lerp(new Color(0f, 0.8f, 1f), Color.white, 0.3f));
                DrawLegendItem("Tier 1", new Color(0f, 0.8f, 1f));
                DrawLegendItem("Tier 2", Color.Lerp(new Color(0f, 0.8f, 1f), Color.black, 0.3f));
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Other:", EditorStyles.miniBoldLabel);
                DrawLegendItem("Station", new Color(0.8f, 0.2f, 0.8f));
                DrawLegendItem("Projectile", new Color(1f, 0.2f, 0.2f));
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
