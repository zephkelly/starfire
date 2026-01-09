using UnityEngine;
using UnityEditor;
using Starfire.Core.Background.Layers;

namespace Starfire.Core.Background.Editor
{
    [CustomEditor(typeof(StarfieldManager))]
    public class StarfieldManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _backgroundDepth;
        private SerializedProperty _scaleMultiplier;
        private SerializedProperty _enableEditorPreview;
        private SerializedProperty _layers;

        private void OnEnable()
        {
            _backgroundDepth = serializedObject.FindProperty("backgroundDepth");
            _scaleMultiplier = serializedObject.FindProperty("scaleMultiplier");
            _enableEditorPreview = serializedObject.FindProperty("enableEditorPreview");
            _layers = serializedObject.FindProperty("layers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Settings
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_backgroundDepth);
            EditorGUILayout.PropertyField(_scaleMultiplier);

            EditorGUILayout.Space();

            // Editor
            EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableEditorPreview);

            EditorGUILayout.Space();

            // Layers
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);

            // Add layer buttons - first row
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Star Layer", GUILayout.Height(25)))
            {
                AddStarLayer();
            }
            if (GUILayout.Button("+ Multi-Star Layer", GUILayout.Height(25)))
            {
                AddMultiStarLayer();
            }
            EditorGUILayout.EndHorizontal();

            // Add layer buttons - second row
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Shooting Stars", GUILayout.Height(25)))
            {
                AddShootingStarLayer();
            }
            if (GUILayout.Button("+ Comet Layer", GUILayout.Height(25)))
            {
                AddCometLayer();
            }
            EditorGUILayout.EndHorizontal();

            // Add layer buttons - third row
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Shaped Star Layer", GUILayout.Height(25)))
            {
                AddShapedStarLayer();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Draw each layer
            for (int i = 0; i < _layers.arraySize; i++)
            {
                DrawLayer(i);
            }

            serializedObject.ApplyModifiedProperties();

            // Force repaint during play mode for live debug updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawLayer(int index)
        {
            var layerProperty = _layers.GetArrayElementAtIndex(index);

            // Check if the layer is null (SerializeReference can be null)
            if (layerProperty.managedReferenceValue == null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Layer {index} (null)", EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _layers.DeleteArrayElementAtIndex(index);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with foldout and controls
            EditorGUILayout.BeginHorizontal();

            layerProperty.isExpanded = EditorGUILayout.Foldout(layerProperty.isExpanded, $"Layer {index}", true);

            // Get layer name directly from the object
            var layer = layerProperty.managedReferenceValue as StarfieldLayer;
            if (layer != null && !string.IsNullOrEmpty(layer.layerName))
            {
                EditorGUILayout.LabelField($"({layer.layerName})", GUILayout.Width(150));
            }

            // Move up/down buttons
            GUI.enabled = index > 0;
            if (GUILayout.Button("▲", GUILayout.Width(25)))
            {
                _layers.MoveArrayElement(index, index - 1);
            }
            GUI.enabled = index < _layers.arraySize - 1;
            if (GUILayout.Button("▼", GUILayout.Width(25)))
            {
                _layers.MoveArrayElement(index, index + 1);
            }
            GUI.enabled = true;

            // Delete button
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                _layers.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            // Draw layer properties if expanded
            if (layerProperty.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Use the default property drawer which handles SerializeReference properly
                var iterator = layerProperty.Copy();
                var endProperty = iterator.GetEndProperty();

                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;

                    // Conditionally disable maxActiveStars when limitActiveStars is false
                    if (layer is ShootingStarLayer && iterator.name == "maxActiveStars")
                    {
                        var limitProp = layerProperty.FindPropertyRelative("limitActiveStars");
                        bool wasEnabled = GUI.enabled;
                        GUI.enabled = wasEnabled && (limitProp != null && limitProp.boolValue);
                        EditorGUILayout.PropertyField(iterator, true);
                        GUI.enabled = wasEnabled;
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }

                // Add spawn button for ShootingStarLayer
                if (layer is ShootingStarLayer shootingLayer)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * 15);

                    GUI.enabled = Application.isPlaying && shootingLayer.IsInitialized;
                    if (GUILayout.Button("Spawn Shooting Star", GUILayout.Height(22)))
                    {
                        shootingLayer.SpawnStar();
                    }
                    GUI.enabled = true;

                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("Enter Play mode to spawn", MessageType.None);
                    }

                    EditorGUILayout.EndHorizontal();

                    // Debug info during Play mode
                    if (Application.isPlaying && shootingLayer.IsInitialized)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Debug Info", EditorStyles.miniLabel);

                        int maxStars = shootingLayer.limitActiveStars ? shootingLayer.maxActiveStars : 8;
                        EditorGUILayout.LabelField($"  Active Stars: {shootingLayer.ActiveStarCount} / {maxStars}");

                        var behaviorCounts = shootingLayer.GetActiveBehaviorCounts();
                        foreach (var kvp in behaviorCounts)
                        {
                            EditorGUILayout.LabelField($"    {kvp.Key}: {kvp.Value}");
                        }
                    }
                }

                // Add spawn button for CometLayer
                if (layer is CometLayer cometLayer)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * 15);

                    GUI.enabled = Application.isPlaying && cometLayer.IsInitialized;
                    if (GUILayout.Button("Spawn Comet", GUILayout.Height(22)))
                    {
                        cometLayer.SpawnComet();
                    }
                    GUI.enabled = true;

                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("Enter Play mode to spawn", MessageType.None);
                    }

                    EditorGUILayout.EndHorizontal();

                    // Debug info during Play mode
                    if (Application.isPlaying && cometLayer.IsInitialized)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Debug Info", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"  Active Comets: {cometLayer.ActiveCometCount}");
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void AddStarLayer()
        {
            // Create a new StarLayer instance with unique seed
            var newLayer = new StarLayer
            {
                layerName = $"Stars {_layers.arraySize + 1}",
                renderBackground = _layers.arraySize == 0,
                // Assign unique seed based on layer count to ensure different star positions per layer
                layerSeed = _layers.arraySize
            };

            // Add to array using SerializeReference
            _layers.arraySize++;
            var newLayerProperty = _layers.GetArrayElementAtIndex(_layers.arraySize - 1);
            newLayerProperty.managedReferenceValue = newLayer;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void AddShootingStarLayer()
        {
            // Create a new ShootingStarLayer instance
            var newLayer = new ShootingStarLayer
            {
                layerName = $"Shooting Stars {_layers.arraySize + 1}",
                parallaxDepth = 0.01f
            };

            // Add to array using SerializeReference
            _layers.arraySize++;
            var newLayerProperty = _layers.GetArrayElementAtIndex(_layers.arraySize - 1);
            newLayerProperty.managedReferenceValue = newLayer;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void AddCometLayer()
        {
            // Create a new CometLayer instance
            var newLayer = new CometLayer
            {
                layerName = $"Comets {_layers.arraySize + 1}",
                parallaxDepth = 0.015f
            };

            // Add to array using SerializeReference
            _layers.arraySize++;
            var newLayerProperty = _layers.GetArrayElementAtIndex(_layers.arraySize - 1);
            newLayerProperty.managedReferenceValue = newLayer;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void AddMultiStarLayer()
        {
            // Create a new MultiStarLayer with 4 default depth configurations
            var newLayer = new MultiStarLayer
            {
                layerName = $"Multi-Stars {_layers.arraySize + 1}",
                renderBackground = _layers.arraySize == 0
            };
            // SetupDefaultDepths is called by the default field initializer

            // Add to array using SerializeReference
            _layers.arraySize++;
            var newLayerProperty = _layers.GetArrayElementAtIndex(_layers.arraySize - 1);
            newLayerProperty.managedReferenceValue = newLayer;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void AddShapedStarLayer()
        {
            // Create a new ShapedStarLayer with default shape configuration
            var newLayer = new ShapedStarLayer
            {
                layerName = $"Shaped Stars {_layers.arraySize + 1}",
                renderBackground = _layers.arraySize == 0,
                layerSeed = _layers.arraySize
            };

            // Add to array using SerializeReference
            _layers.arraySize++;
            var newLayerProperty = _layers.GetArrayElementAtIndex(_layers.arraySize - 1);
            newLayerProperty.managedReferenceValue = newLayer;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
