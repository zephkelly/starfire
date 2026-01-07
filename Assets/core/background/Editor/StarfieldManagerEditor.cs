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

            // Add layer button
            if (GUILayout.Button("+ Add Star Layer", GUILayout.Height(25)))
            {
                AddStarLayer();
            }

            EditorGUILayout.Space();

            // Draw each layer
            for (int i = 0; i < _layers.arraySize; i++)
            {
                DrawLayer(i);
            }

            serializedObject.ApplyModifiedProperties();
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
                    EditorGUILayout.PropertyField(iterator, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void AddStarLayer()
        {
            // Create a new StarLayer instance
            var newLayer = new StarLayer
            {
                layerName = $"Stars {_layers.arraySize + 1}",
                renderBackground = _layers.arraySize == 0
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
