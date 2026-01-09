using UnityEngine;
using UnityEditor;
using Starfire.Core;

namespace Starfire.Core.Editor
{
    /// <summary>
    /// Custom property drawer that displays float fields with configurable decimal precision.
    /// </summary>
    [CustomPropertyDrawer(typeof(HighPrecisionAttribute))]
    public class HighPrecisionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Float)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var attr = (HighPrecisionAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            // Display the current value with full precision
            string format = $"F{attr.DecimalPlaces}";
            string valueString = property.floatValue.ToString(format);

            // Create a text field for input
            EditorGUI.BeginChangeCheck();

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);
            string newValueString = EditorGUI.TextField(fieldRect, valueString);

            if (EditorGUI.EndChangeCheck())
            {
                if (float.TryParse(newValueString, out float newValue))
                {
                    // Enforce minimum value
                    newValue = Mathf.Max(newValue, attr.MinValue);
                    property.floatValue = newValue;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
