using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace StarfireV2.Editor
{
    [CustomEditor(typeof(ShipController))]
    public class ShipControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _slotConfigurations;
        private readonly Dictionary<ShipModuleCategory, bool> _categoryFoldouts = new();
        private readonly List<int> _indicesToRemove = new();
        private readonly Dictionary<int, bool> _overrideFoldouts = new();

        private GUIStyle _categoryBoxStyle;
        private GUIStyle _categoryHeaderStyle;
        private GUIStyle _overrideBoxStyle;

        private void OnEnable()
        {
            _slotConfigurations = serializedObject.FindProperty("_slotConfigurations");

            foreach (ShipModuleCategory category in Enum.GetValues(typeof(ShipModuleCategory)))
                _categoryFoldouts[category] = true;
        }

        private void InitializeStyles()
        {
            if (_categoryBoxStyle == null)
            {
                _categoryBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(0, 0, 4, 4)
                };
            }

            if (_categoryHeaderStyle == null)
            {
                _categoryHeaderStyle = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
            }

            if (_overrideBoxStyle == null)
            {
                _overrideBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 6, 4, 4),
                    margin = new RectOffset(16, 4, 2, 2)
                };
            }
        }

        public override void OnInspectorGUI()
        {
            // Handle multi-object editing
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("Module slot editing is not supported when multiple objects are selected.", MessageType.Info);
                DrawPropertiesExcluding(serializedObject, "_slotConfigurations");
                return;
            }

            InitializeStyles();
            serializedObject.Update();

            // Draw default properties except slot configurations
            DrawPropertiesExcluding(serializedObject, "_slotConfigurations");

            EditorGUILayout.Space(10);

            DrawModuleSlotsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawModuleSlotsSection()
        {
            // Section header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Module Slot Configurations", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Total: {_slotConfigurations.arraySize}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Separator line
            var lineRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            EditorGUILayout.Space(6);

            _indicesToRemove.Clear();

            // Group slots by category
            var slotsByCategory = GroupSlotsByCategory();

            // Check if we have any slots at all
            bool hasAnySlots = false;
            foreach (var category in slotsByCategory.Keys)
            {
                if (slotsByCategory[category].Count > 0)
                {
                    hasAnySlots = true;
                    break;
                }
            }

            if (!hasAnySlots)
            {
                EditorGUILayout.HelpBox("No module slots configured. Click 'Add Module Slot' to add slots.", MessageType.Info);
            }
            else
            {
                // Draw each category that has slots
                foreach (ShipModuleCategory category in Enum.GetValues(typeof(ShipModuleCategory)))
                {
                    if (!slotsByCategory.TryGetValue(category, out var indices) || indices.Count == 0)
                        continue;

                    DrawCategoryGroup(category, indices);
                    EditorGUILayout.Space(4);
                }
            }

            // Apply deferred deletions (in reverse order to maintain indices)
            foreach (int idx in _indicesToRemove.OrderByDescending(i => i))
                _slotConfigurations.DeleteArrayElementAtIndex(idx);

            EditorGUILayout.Space(8);
            DrawAddSlotButton();
        }

        private Dictionary<ShipModuleCategory, List<int>> GroupSlotsByCategory()
        {
            var groups = new Dictionary<ShipModuleCategory, List<int>>();

            foreach (ShipModuleCategory category in Enum.GetValues(typeof(ShipModuleCategory)))
                groups[category] = new List<int>();

            for (int i = 0; i < _slotConfigurations.arraySize; i++)
            {
                var slotProp = _slotConfigurations.GetArrayElementAtIndex(i);
                var typeIdProp = slotProp.FindPropertyRelative("typeId");
                var typeId = (ShipModuleTypeId)typeIdProp.intValue;

                try
                {
                    var category = EntityModuleHierarchyRegistry.GetCategory(typeId);
                    groups[category].Add(i);
                }
                catch
                {
                    // If category lookup fails, add to Utility as fallback
                    groups[ShipModuleCategory.Utility].Add(i);
                }
            }

            return groups;
        }

        private void DrawCategoryGroup(ShipModuleCategory category, List<int> indices)
        {
            var categoryColor = ModuleEditorUtility.GetCategoryColor(category);

            // Draw colored border/background for the category
            EditorGUILayout.BeginVertical(_categoryBoxStyle);

            // Header row with color bar
            EditorGUILayout.BeginHorizontal();

            // Colored sidebar indicator
            var sidebarRect = GUILayoutUtility.GetRect(4, 20, GUILayout.Width(4));
            EditorGUI.DrawRect(sidebarRect, categoryColor);

            GUILayout.Space(6);

            // Foldout with category name and count
            _categoryFoldouts[category] = EditorGUILayout.Foldout(
                _categoryFoldouts[category],
                $"{category} ({indices.Count})",
                true,
                _categoryHeaderStyle
            );

            EditorGUILayout.EndHorizontal();

            // Draw slots if expanded
            if (_categoryFoldouts[category])
            {
                EditorGUILayout.Space(4);

                // Indent the slots
                EditorGUI.indentLevel++;

                foreach (int idx in indices)
                {
                    // Slot row with slight background
                    var slotBgColor = new Color(0, 0, 0, 0.1f);
                    var slotRect = EditorGUILayout.BeginHorizontal();
                    EditorGUI.DrawRect(slotRect, slotBgColor);

                    var slotProp = _slotConfigurations.GetArrayElementAtIndex(idx);

                    // Ensure slot has an ID, auto-generate if empty
                    var slotIdProp = slotProp.FindPropertyRelative("slotId");
                    if (string.IsNullOrEmpty(slotIdProp.stringValue))
                    {
                        var typeIdProp = slotProp.FindPropertyRelative("typeId");
                        var typeId = (ShipModuleTypeId)typeIdProp.intValue;
                        slotIdProp.stringValue = GenerateUniqueSlotId(GenerateBaseSlotId(typeId));
                    }

                    // Draw all fields except overrideData and hasOverrides (we handle those below)
                    EditorGUILayout.PropertyField(slotProp, GUIContent.none, true);

                    // Delete button
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("×", GUILayout.Width(22), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    {
                        _indicesToRemove.Add(idx);
                    }
                    GUI.backgroundColor = oldColor;

                    EditorGUILayout.EndHorizontal();

                    // Draw override foldout if slot is expanded and has a default module
                    if (slotProp.isExpanded)
                    {
                        DrawOverrideSection(slotProp, idx);
                        EditorGUILayout.Space(4);
                    }
                    else
                    {
                        EditorGUILayout.Space(2);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOverrideSection(SerializedProperty slotProp, int index)
        {
            var defaultModuleProp = slotProp.FindPropertyRelative("defaultModule");
            var overrideDataProp = slotProp.FindPropertyRelative("overrideData");
            var hasOverridesProp = slotProp.FindPropertyRelative("hasOverrides");

            // No default module assigned — nothing to override
            if (defaultModuleProp.objectReferenceValue == null)
            {
                // Clear override data if module was removed
                if (overrideDataProp.managedReferenceValue != null)
                {
                    overrideDataProp.managedReferenceValue = null;
                    hasOverridesProp.boolValue = false;
                }
                return;
            }

            var configSO = defaultModuleProp.objectReferenceValue;
            if (configSO is not IShipModuleConfig moduleConfig)
                return;

            // Auto-populate override data if it's null or type changed
            if (overrideDataProp.managedReferenceValue == null)
            {
                overrideDataProp.managedReferenceValue = moduleConfig.ToData();
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();

                // Re-fetch properties since Update() invalidates previous references
                overrideDataProp = slotProp.FindPropertyRelative("overrideData");
                hasOverridesProp = slotProp.FindPropertyRelative("hasOverrides");
            }

            // Draw the override section
            EditorGUILayout.BeginVertical(_overrideBoxStyle);

            EditorGUILayout.BeginHorizontal();

            // Foldout
            if (!_overrideFoldouts.ContainsKey(index))
                _overrideFoldouts[index] = false;

            _overrideFoldouts[index] = EditorGUILayout.Foldout(
                _overrideFoldouts[index],
                "Overrides",
                true
            );

            // hasOverrides toggle
            var prevEnabled = hasOverridesProp.boolValue;
            hasOverridesProp.boolValue = EditorGUILayout.Toggle(hasOverridesProp.boolValue, GUILayout.Width(16));

            // Label to clarify the toggle
            var labelStyle = new GUIStyle(EditorStyles.miniLabel);
            if (!hasOverridesProp.boolValue)
                labelStyle.normal.textColor = Color.gray;
            EditorGUILayout.LabelField(hasOverridesProp.boolValue ? "Active" : "Inactive", labelStyle, GUILayout.Width(44));

            // Reset button
            if (GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                overrideDataProp.managedReferenceValue = moduleConfig.ToData();
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            EditorGUILayout.EndHorizontal();

            // Draw override data fields when expanded
            if (_overrideFoldouts[index] && overrideDataProp.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;

                // Iterate through visible children of the SerializeReference property
                var iter = overrideDataProp.Copy();
                var endProp = overrideDataProp.GetEndProperty();
                bool enterChildren = true;

                while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, endProp))
                {
                    enterChildren = false;
                    EditorGUILayout.PropertyField(iter, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAddSlotButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);

            if (GUILayout.Button("+ Add Module Slot", GUILayout.Width(150), GUILayout.Height(24)))
            {
                ShowAddSlotMenu();
            }

            GUI.backgroundColor = oldBgColor;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddSlotMenu()
        {
            var menu = ModuleEditorUtility.BuildTypeSelectionMenu(AddNewSlot);
            menu.ShowAsContext();
        }

        private void AddNewSlot(ShipModuleTypeId typeId)
        {
            serializedObject.Update();

            int newIndex = _slotConfigurations.arraySize;
            _slotConfigurations.InsertArrayElementAtIndex(newIndex);

            var newSlot = _slotConfigurations.GetArrayElementAtIndex(newIndex);

            // Generate unique slot ID
            string baseId = GenerateBaseSlotId(typeId);
            string uniqueId = GenerateUniqueSlotId(baseId);

            newSlot.FindPropertyRelative("slotId").stringValue = uniqueId;
            newSlot.FindPropertyRelative("typeId").intValue = (int)typeId;
            newSlot.FindPropertyRelative("isRequired").boolValue = false;
            newSlot.FindPropertyRelative("isAvailable").boolValue = true;
            newSlot.FindPropertyRelative("defaultModule").objectReferenceValue = null;
            newSlot.FindPropertyRelative("hasOverrides").boolValue = false;
            newSlot.FindPropertyRelative("overrideData").managedReferenceValue = null;

            serializedObject.ApplyModifiedProperties();
        }

        private string GenerateBaseSlotId(ShipModuleTypeId typeId)
        {
            string displayName = ModuleEditorUtility.GetDisplayName(typeId);
            // Convert to snake_case
            string snakeCase = Regex.Replace(displayName, @"\s+", "_").ToLowerInvariant();
            return snakeCase;
        }

        private string GenerateUniqueSlotId(string baseId)
        {
            var existingIds = new HashSet<string>();

            for (int i = 0; i < _slotConfigurations.arraySize; i++)
            {
                var slotProp = _slotConfigurations.GetArrayElementAtIndex(i);
                var slotId = slotProp.FindPropertyRelative("slotId").stringValue;
                existingIds.Add(slotId);
            }

            // Try base ID first
            if (!existingIds.Contains(baseId))
                return baseId;

            // Append incrementing number
            int counter = 1;
            while (existingIds.Contains($"{baseId}_{counter}"))
                counter++;

            return $"{baseId}_{counter}";
        }
    }
}
