#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StarfireV2.Editor
{
    /// <summary>
    /// Popup window for editing node parameters via double-click.
    /// Uses reflection to dynamically draw fields for any parameter type.
    /// </summary>
    public class BTNodePopup : PopupWindowContent
    {
        private readonly BTNodeView _nodeView;
        private readonly BehaviorTreeAsset _treeAsset;
        private Vector2 _scrollPosition;
        private const float PopupWidth = 280f;
        private const float PopupMaxHeight = 400f;

        public BTNodePopup(BTNodeView nodeView, BehaviorTreeAsset treeAsset)
        {
            _nodeView = nodeView;
            _treeAsset = treeAsset;
        }

        public override Vector2 GetWindowSize()
        {
            // Calculate height based on number of fields
            float height = 60f; // Base height for header

            // Add height for tick interval if action node
            if (_nodeView?.NodeData?.nodeType == BTNodeType.Action)
            {
                height += 26f;
            }

            if (_nodeView?.NodeData?.parameters != null)
            {
                var fields = _nodeView.NodeData.parameters.GetType().GetFields(
                    BindingFlags.Public | BindingFlags.Instance);
                height += fields.Length * 22f + 20f;
            }

            height = Mathf.Min(height, PopupMaxHeight);
            return new Vector2(PopupWidth, height);
        }

        public override void OnGUI(Rect rect)
        {
            if (_nodeView == null || _nodeView.NodeData == null)
            {
                EditorGUILayout.LabelField("No node selected");
                return;
            }

            var data = _nodeView.NodeData;

            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(GetNodeDisplayName(data), EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // Tick Interval for action nodes
            if (data.nodeType == BTNodeType.Action)
            {
                EditorGUI.BeginChangeCheck();
                int newTickInterval = EditorGUILayout.IntSlider(
                    new GUIContent("Tick Interval", "1 = every tick, higher = skip ticks"),
                    data.tickInterval,
                    1,
                    10);
                if (EditorGUI.EndChangeCheck())
                {
                    data.tickInterval = newTickInterval;
                    EditorUtility.SetDirty(_treeAsset);
                }
                EditorGUILayout.Space(4);
            }

            // Parameters
            if (data.parameters != null)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawParametersWithReflection(data);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("No parameters", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private string GetNodeDisplayName(BTNodeData data)
        {
            if (data.nodeType == BTNodeType.Action)
            {
                return data.actionType;
            }
            return data.nodeType.ToString();
        }

        private void DrawParametersWithReflection(BTNodeData data)
        {
            var parameters = data.parameters;
            if (parameters == null) return;

            var type = parameters.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            EditorGUI.BeginChangeCheck();

            foreach (var field in fields)
            {
                DrawField(field, parameters);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_treeAsset);
                _nodeView.UpdateVisuals();
            }
        }

        private void DrawField(FieldInfo field, object target)
        {
            var fieldType = field.FieldType;
            var fieldName = ObjectNames.NicifyVariableName(field.Name);
            var currentValue = field.GetValue(target);

            object newValue = currentValue;

            // Handle different field types
            if (fieldType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(fieldName, (string)currentValue ?? "");
            }
            else if (fieldType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(fieldName, (int)currentValue);
            }
            else if (fieldType == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(fieldName, (float)currentValue);
            }
            else if (fieldType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(fieldName, (bool)currentValue);
            }
            else if (fieldType == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(fieldName, (Vector2)currentValue);
            }
            else if (fieldType == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(fieldName, (Vector3)currentValue);
            }
            else if (fieldType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(fieldName, (Enum)currentValue);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                newValue = EditorGUILayout.ObjectField(fieldName, (UnityEngine.Object)currentValue, fieldType, false);
            }
            else
            {
                // Unsupported type - show as label
                EditorGUILayout.LabelField(fieldName, currentValue?.ToString() ?? "(null)");
                return;
            }

            // Update value if changed
            if (!Equals(newValue, currentValue))
            {
                field.SetValue(target, newValue);
            }
        }
    }
}
#endif
