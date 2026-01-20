using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StarfireV2.Editor
{
    [CustomPropertyDrawer(typeof(EntityControllerDriverStack))]
    public class EntityControllerDriverStackDrawer : PropertyDrawer
    {
        private static Type[] _driverTypes;
        private static string[] _driverTypeNames;
        private int _selectedTypeIndex;

        // Track expanded state per element by path
        private Dictionary<string, bool> _expandedStates = new();

        private static void CacheDriverTypes()
        {
            if (_driverTypes != null) return;

            var interfaceType = typeof(IEntityControllerDriver);
            _driverTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => interfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToArray();

            _driverTypeNames = _driverTypes.Select(t => t.Name).ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var driversProp = property.FindPropertyRelative("drivers");
            float height = EditorGUIUtility.singleLineHeight + 4; // Header

            if (property.isExpanded)
            {
                height += EditorGUIUtility.singleLineHeight + 4; // Add driver row

                if (driversProp != null)
                {
                    for (int i = 0; i < driversProp.arraySize; i++)
                    {
                        var element = driversProp.GetArrayElementAtIndex(i);
                        height += EditorGUIUtility.singleLineHeight + 2; // Driver header row

                        // Add height for expanded properties
                        string key = element.propertyPath;
                        if (_expandedStates.TryGetValue(key, out bool isExpanded) && isExpanded && element.managedReferenceValue != null)
                        {
                            height += GetChildPropertiesHeight(element);
                        }
                    }
                }
            }

            return height;
        }

        private float GetChildPropertiesHeight(SerializedProperty element)
        {
            float height = 0f;
            var iterator = element.Copy();
            var endProperty = element.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator, true) + 2;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheDriverTypes();

            EditorGUI.BeginProperty(position, label, property);

            var driversProp = property.FindPropertyRelative("drivers");

            // Foldout header
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + EditorGUIUtility.singleLineHeight + 4;

                // Add driver row
                var addRowRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                DrawAddDriverRow(addRowRect, driversProp);
                y += EditorGUIUtility.singleLineHeight + 4;

                // Draw existing drivers
                if (driversProp != null)
                {
                    for (int i = 0; i < driversProp.arraySize; i++)
                    {
                        var element = driversProp.GetArrayElementAtIndex(i);
                        float itemHeight = EditorGUIUtility.singleLineHeight;

                        string key = element.propertyPath;
                        bool isExpanded = _expandedStates.TryGetValue(key, out bool exp) && exp;

                        if (isExpanded && element.managedReferenceValue != null)
                        {
                            itemHeight += GetChildPropertiesHeight(element);
                        }

                        var itemRect = new Rect(position.x, y, position.width, itemHeight);
                        DrawDriverItem(itemRect, driversProp, i);
                        y += itemHeight + 2;
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private void DrawAddDriverRow(Rect rect, SerializedProperty driversProp)
        {
            var indentedRect = EditorGUI.IndentedRect(rect);

            float dropdownWidth = indentedRect.width - 60;
            float buttonWidth = 55;

            var dropdownRect = new Rect(indentedRect.x, indentedRect.y, dropdownWidth, indentedRect.height);
            var buttonRect = new Rect(indentedRect.x + dropdownWidth + 5, indentedRect.y, buttonWidth, indentedRect.height);

            if (_driverTypeNames != null && _driverTypeNames.Length > 0)
            {
                _selectedTypeIndex = EditorGUI.Popup(dropdownRect, _selectedTypeIndex, _driverTypeNames);

                if (GUI.Button(buttonRect, "Add"))
                {
                    var type = _driverTypes[_selectedTypeIndex];
                    var instance = Activator.CreateInstance(type);

                    driversProp.arraySize++;
                    var newElement = driversProp.GetArrayElementAtIndex(driversProp.arraySize - 1);
                    newElement.managedReferenceValue = instance;
                    driversProp.serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                EditorGUI.LabelField(dropdownRect, "No driver types found");
            }
        }

        private void DrawDriverItem(Rect rect, SerializedProperty driversProp, int index)
        {
            var indentedRect = EditorGUI.IndentedRect(rect);
            var element = driversProp.GetArrayElementAtIndex(index);

            float removeButtonWidth = 20;
            float foldoutWidth = indentedRect.width - removeButtonWidth - 5;

            var foldoutRect = new Rect(indentedRect.x, indentedRect.y, foldoutWidth, EditorGUIUtility.singleLineHeight);
            var removeRect = new Rect(indentedRect.x + foldoutWidth + 5, indentedRect.y, removeButtonWidth, EditorGUIUtility.singleLineHeight);

            // Get type name from managed reference
            string typeName = "null";
            if (element.managedReferenceValue != null)
            {
                typeName = element.managedReferenceValue.GetType().Name;
            }

            // Track expanded state
            string key = element.propertyPath;
            if (!_expandedStates.ContainsKey(key))
            {
                _expandedStates[key] = false;
            }

            // Foldout for driver
            _expandedStates[key] = EditorGUI.Foldout(foldoutRect, _expandedStates[key], $"[{index}] {typeName}", true);

            if (GUI.Button(removeRect, "X"))
            {
                driversProp.DeleteArrayElementAtIndex(index);
                driversProp.serializedObject.ApplyModifiedProperties();
                return;
            }

            // Draw child properties when expanded
            if (_expandedStates[key] && element.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;

                var iterator = element.Copy();
                var endProperty = element.GetEndProperty();

                bool enterChildren = true;
                float yOffset = EditorGUIUtility.singleLineHeight + 2;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    enterChildren = false;

                    float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                    var propRect = new Rect(
                        indentedRect.x,
                        indentedRect.y + yOffset,
                        indentedRect.width,
                        propHeight
                    );

                    EditorGUI.PropertyField(propRect, iterator, true);
                    yOffset += propHeight + 2;
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
