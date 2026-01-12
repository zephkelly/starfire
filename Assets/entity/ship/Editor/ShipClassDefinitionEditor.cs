using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Starfire.Entity.Modules;

namespace Starfire.Entity.Editor
{
    [CustomEditor(typeof(ShipClassDefinition))]
    public class ShipClassDefinitionEditor : UnityEditor.Editor
    {
        // Identity properties
        private SerializedProperty _classId;
        private SerializedProperty _className;
        private SerializedProperty _description;
        private SerializedProperty _classIcon;

        // Multi-slot properties
        private SerializedProperty _multiSlots;
        private SerializedProperty _categoryLimits;

        // Other properties
        private SerializedProperty _capabilities;
        private SerializedProperty _prefabOverride;
        private SerializedProperty _spriteScale;

        // Foldout states
        private bool _identityFoldout = true;
        private bool _multiSlotsFoldout = true;
        private bool _limitsFoldout = true;
        private bool _capabilitiesFoldout = true;
        private bool _visualFoldout = true;

        // Multi-slot expansion tracking
        private readonly Dictionary<string, bool> _multiSlotExpanded = new();
        private readonly Dictionary<ModuleCategory, bool> _categoryExpanded = new();
        private readonly Dictionary<ModuleSubCategory, bool> _subCategoryExpanded = new();

        private void OnEnable()
        {
            _classId = serializedObject.FindProperty("classId");
            _className = serializedObject.FindProperty("className");
            _description = serializedObject.FindProperty("description");
            _classIcon = serializedObject.FindProperty("classIcon");
            _multiSlots = serializedObject.FindProperty("multiSlots");
            _categoryLimits = serializedObject.FindProperty("categoryLimits");
            _capabilities = serializedObject.FindProperty("capabilities");
            _prefabOverride = serializedObject.FindProperty("prefabOverride");
            _spriteScale = serializedObject.FindProperty("spriteScale");

            // Initialize category expansion states
            foreach (ModuleCategory cat in Enum.GetValues(typeof(ModuleCategory)))
            {
                if (!_categoryExpanded.ContainsKey(cat))
                    _categoryExpanded[cat] = true;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Identity Section
            _identityFoldout = EditorGUILayout.Foldout(_identityFoldout, "Identity", true, EditorStyles.foldoutHeader);
            if (_identityFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_classId);
                EditorGUILayout.PropertyField(_className);
                EditorGUILayout.PropertyField(_description);
                EditorGUILayout.PropertyField(_classIcon);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Module Slots Section
            _multiSlotsFoldout = EditorGUILayout.Foldout(_multiSlotsFoldout, "Module Slots", true, EditorStyles.foldoutHeader);
            if (_multiSlotsFoldout)
            {
                DrawMultiSlotSection();
            }

            EditorGUILayout.Space(5);

            // Category Limits Section
            _limitsFoldout = EditorGUILayout.Foldout(_limitsFoldout, "Category Limits", true, EditorStyles.foldoutHeader);
            if (_limitsFoldout)
            {
                DrawCategoryLimitsSection();
            }

            EditorGUILayout.Space(5);

            // Capabilities Section
            _capabilitiesFoldout = EditorGUILayout.Foldout(_capabilitiesFoldout, "Capabilities", true, EditorStyles.foldoutHeader);
            if (_capabilitiesFoldout)
            {
                EditorGUI.indentLevel++;
                DrawCapabilitiesArray();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Visual Section
            _visualFoldout = EditorGUILayout.Foldout(_visualFoldout, "Visual", true, EditorStyles.foldoutHeader);
            if (_visualFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_prefabOverride);
                EditorGUILayout.PropertyField(_spriteScale);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCapabilitiesArray()
        {
            // Draw each element with remove button
            for (int i = 0; i < _capabilities.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(
                    _capabilities.GetArrayElementAtIndex(i),
                    GUIContent.none
                );

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    _capabilities.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            // Add button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                _capabilities.InsertArrayElementAtIndex(_capabilities.arraySize);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        // =====================================================
        // Multi-Slot System UI
        // =====================================================

        private void DrawMultiSlotSection()
        {
            EditorGUI.indentLevel++;

            int slotCount = _multiSlots.arraySize;
            EditorGUILayout.LabelField($"Configured Multi-Slots: {slotCount}", EditorStyles.miniLabel);

            // Validate limits and show warning if exceeded
            var shipDef = target as ShipClassDefinition;
            if (shipDef != null && !shipDef.ValidateLimits(out string limitError))
            {
                EditorGUILayout.HelpBox(limitError, MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // Group slots by category
            var slotsByCategory = new Dictionary<ModuleCategory, List<int>>();
            for (int i = 0; i < _multiSlots.arraySize; i++)
            {
                var slotProp = _multiSlots.GetArrayElementAtIndex(i);
                var moduleTypeProp = slotProp.FindPropertyRelative("moduleType");
                var moduleType = (ModuleTypeId)moduleTypeProp.intValue;
                var category = ModuleHierarchyRegistry.GetCategory(moduleType);

                if (!slotsByCategory.ContainsKey(category))
                    slotsByCategory[category] = new List<int>();
                slotsByCategory[category].Add(i);
            }

            // Draw each category
            foreach (ModuleCategory category in Enum.GetValues(typeof(ModuleCategory)))
            {
                if (!slotsByCategory.ContainsKey(category) || slotsByCategory[category].Count == 0)
                    continue;

                DrawCategoryGroup(category, slotsByCategory[category]);
            }

            EditorGUILayout.Space(10);

            // Add slot button
            DrawAddMultiSlotButton();

            EditorGUI.indentLevel--;
        }

        private void DrawCategoryGroup(ModuleCategory category, List<int> slotIndices)
        {
            if (!_categoryExpanded.ContainsKey(category))
                _categoryExpanded[category] = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Category header
            EditorGUILayout.BeginHorizontal();
            _categoryExpanded[category] = EditorGUILayout.Foldout(
                _categoryExpanded[category],
                $"{category} ({slotIndices.Count})",
                true,
                EditorStyles.foldoutHeader
            );

            // Show limit info if exists
            var shipDef = target as ShipClassDefinition;
            var categoryLimit = shipDef?.GetLimitFor(category);
            if (categoryLimit != null)
            {
                int count = slotIndices.Count;
                var color = count > categoryLimit.maxCount ? Color.red : Color.green;
                var oldColor = GUI.color;
                GUI.color = color;
                GUILayout.Label($"[{count}/{categoryLimit.maxCount}]", EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.color = oldColor;
            }

            EditorGUILayout.EndHorizontal();

            if (_categoryExpanded[category])
            {
                EditorGUI.indentLevel++;

                // Group by subcategory
                var slotsBySubCategory = new Dictionary<ModuleSubCategory, List<int>>();
                foreach (int idx in slotIndices)
                {
                    var slotProp = _multiSlots.GetArrayElementAtIndex(idx);
                    var moduleTypeProp = slotProp.FindPropertyRelative("moduleType");
                    var moduleType = (ModuleTypeId)moduleTypeProp.intValue;
                    var subCategory = ModuleHierarchyRegistry.GetSubCategory(moduleType);

                    if (!slotsBySubCategory.ContainsKey(subCategory))
                        slotsBySubCategory[subCategory] = new List<int>();
                    slotsBySubCategory[subCategory].Add(idx);
                }

                // Draw each subcategory
                foreach (var kvp in slotsBySubCategory)
                {
                    DrawSubCategoryGroup(kvp.Key, kvp.Value);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSubCategoryGroup(ModuleSubCategory subCategory, List<int> slotIndices)
        {
            if (!_subCategoryExpanded.ContainsKey(subCategory))
                _subCategoryExpanded[subCategory] = true;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // SubCategory header
            EditorGUILayout.BeginHorizontal();
            _subCategoryExpanded[subCategory] = EditorGUILayout.Foldout(
                _subCategoryExpanded[subCategory],
                $"{subCategory} ({slotIndices.Count})",
                true
            );

            // Show limit info if exists
            var shipDef = target as ShipClassDefinition;
            var subCatLimit = shipDef?.GetLimitFor(subCategory);
            if (subCatLimit != null)
            {
                int count = slotIndices.Count;
                var color = count > subCatLimit.maxCount ? Color.red : Color.green;
                var oldColor = GUI.color;
                GUI.color = color;
                GUILayout.Label($"[{count}/{subCatLimit.maxCount}]", EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.color = oldColor;
            }

            EditorGUILayout.EndHorizontal();

            if (_subCategoryExpanded[subCategory])
            {
                EditorGUI.indentLevel++;

                // Draw each slot
                var indicesToRemove = new List<int>();
                foreach (int idx in slotIndices)
                {
                    if (DrawMultiSlotEntry(idx))
                    {
                        indicesToRemove.Add(idx);
                    }
                }

                // Remove deleted slots (in reverse order to maintain indices)
                foreach (int idx in indicesToRemove.OrderByDescending(i => i))
                {
                    _multiSlots.DeleteArrayElementAtIndex(idx);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private bool DrawMultiSlotEntry(int index)
        {
            var slotProp = _multiSlots.GetArrayElementAtIndex(index);
            var slotIdProp = slotProp.FindPropertyRelative("slotId");
            var moduleTypeProp = slotProp.FindPropertyRelative("moduleType");
            var isRequiredProp = slotProp.FindPropertyRelative("isRequired");
            var defaultModuleProp = slotProp.FindPropertyRelative("defaultModule");
            var displayNameOverrideProp = slotProp.FindPropertyRelative("displayNameOverride");

            string slotId = slotIdProp.stringValue;
            var moduleType = (ModuleTypeId)moduleTypeProp.intValue;
            string displayName = ModuleHierarchyRegistry.GetDisplayName(moduleType);

            if (!_multiSlotExpanded.ContainsKey(slotId))
                _multiSlotExpanded[slotId] = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row
            EditorGUILayout.BeginHorizontal();

            _multiSlotExpanded[slotId] = EditorGUILayout.Foldout(
                _multiSlotExpanded[slotId],
                string.IsNullOrEmpty(slotId) ? $"[{displayName}]" : slotId,
                true
            );

            // Required badge
            if (isRequiredProp.boolValue)
            {
                GUILayout.Label("REQ", EditorStyles.miniLabel, GUILayout.Width(30));
            }

            // Module type badge
            GUILayout.Label(displayName, EditorStyles.miniLabel, GUILayout.Width(100));

            // Remove button
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true; // Signal deletion
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Expanded content
            if (_multiSlotExpanded[slotId])
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(slotIdProp, new GUIContent("Slot ID"));
                EditorGUILayout.PropertyField(moduleTypeProp, new GUIContent("Module Type"));
                EditorGUILayout.PropertyField(isRequiredProp, new GUIContent("Required"));
                EditorGUILayout.PropertyField(displayNameOverrideProp, new GUIContent("Display Name Override"));
                EditorGUILayout.PropertyField(defaultModuleProp, new GUIContent("Default Module"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return false;
        }

        private void DrawAddMultiSlotButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("+ Add Module Slot", GUILayout.Width(150)))
            {
                ShowAddMultiSlotMenu();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddMultiSlotMenu()
        {
            var menu = new GenericMenu();

            foreach (ModuleCategory category in Enum.GetValues(typeof(ModuleCategory)))
            {
                var typesInCategory = ModuleHierarchyRegistry.GetTypesInCategory(category);

                foreach (var subCategory in ModuleHierarchyRegistry.GetSubCategoriesInCategory(category))
                {
                    var typesInSubCategory = ModuleHierarchyRegistry.GetTypesInSubCategory(subCategory);

                    foreach (var typeId in typesInSubCategory)
                    {
                        var capturedType = typeId;
                        string displayName = ModuleHierarchyRegistry.GetDisplayName(typeId);
                        menu.AddItem(
                            new GUIContent($"{category}/{subCategory}/{displayName}"),
                            false,
                            () => AddNewMultiSlot(capturedType)
                        );
                    }
                }
            }

            menu.ShowAsContext();
        }

        private void AddNewMultiSlot(ModuleTypeId moduleType)
        {
            serializedObject.Update();

            int newIndex = _multiSlots.arraySize;
            _multiSlots.InsertArrayElementAtIndex(newIndex);

            var newSlot = _multiSlots.GetArrayElementAtIndex(newIndex);

            // Generate unique slot ID
            string baseId = ModuleHierarchyRegistry.GetDisplayName(moduleType).ToLower().Replace(" ", "_");
            int suffix = 1;
            string slotId = baseId;
            while (SlotIdExists(slotId))
            {
                slotId = $"{baseId}_{suffix}";
                suffix++;
            }

            newSlot.FindPropertyRelative("slotId").stringValue = slotId;
            newSlot.FindPropertyRelative("moduleType").intValue = (int)moduleType;
            newSlot.FindPropertyRelative("isRequired").boolValue = false;
            newSlot.FindPropertyRelative("defaultModule").objectReferenceValue = null;
            newSlot.FindPropertyRelative("displayNameOverride").stringValue = "";

            _multiSlotExpanded[slotId] = true;

            serializedObject.ApplyModifiedProperties();
        }

        private bool SlotIdExists(string slotId)
        {
            for (int i = 0; i < _multiSlots.arraySize; i++)
            {
                var existingId = _multiSlots.GetArrayElementAtIndex(i).FindPropertyRelative("slotId").stringValue;
                if (existingId == slotId) return true;
            }
            return false;
        }

        // =====================================================
        // Category Limits UI
        // =====================================================

        private void DrawCategoryLimitsSection()
        {
            EditorGUI.indentLevel++;

            int limitCount = _categoryLimits.arraySize;
            EditorGUILayout.LabelField($"Configured Limits: {limitCount}", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Draw existing limits
            var indicesToRemove = new List<int>();
            for (int i = 0; i < _categoryLimits.arraySize; i++)
            {
                if (DrawCategoryLimitEntry(i))
                {
                    indicesToRemove.Add(i);
                }
            }

            // Remove deleted limits
            foreach (int idx in indicesToRemove.OrderByDescending(i => i))
            {
                _categoryLimits.DeleteArrayElementAtIndex(idx);
            }

            EditorGUILayout.Space(10);

            // Add limit button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("+ Add Limit", GUILayout.Width(100)))
            {
                AddNewCategoryLimit();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private bool DrawCategoryLimitEntry(int index)
        {
            var limitProp = _categoryLimits.GetArrayElementAtIndex(index);
            var scopeProp = limitProp.FindPropertyRelative("scope");
            var categoryProp = limitProp.FindPropertyRelative("category");
            var subCategoryProp = limitProp.FindPropertyRelative("subCategory");
            var moduleTypeProp = limitProp.FindPropertyRelative("moduleType");
            var maxCountProp = limitProp.FindPropertyRelative("maxCount");

            var scope = (CategoryLimit.LimitScope)scopeProp.enumValueIndex;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row with summary
            EditorGUILayout.BeginHorizontal();

            string scopeTarget = scope switch
            {
                CategoryLimit.LimitScope.Category => ((ModuleCategory)categoryProp.enumValueIndex).ToString(),
                CategoryLimit.LimitScope.SubCategory => ((ModuleSubCategory)subCategoryProp.enumValueIndex).ToString(),
                CategoryLimit.LimitScope.ModuleType => ModuleHierarchyRegistry.GetDisplayName((ModuleTypeId)moduleTypeProp.intValue),
                _ => "Unknown"
            };

            EditorGUILayout.LabelField($"Max {maxCountProp.intValue} {scopeTarget}", EditorStyles.boldLabel);

            // Remove button
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Properties
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(scopeProp, new GUIContent("Scope"));

            // Show relevant property based on scope
            switch (scope)
            {
                case CategoryLimit.LimitScope.Category:
                    EditorGUILayout.PropertyField(categoryProp, new GUIContent("Category"));
                    break;
                case CategoryLimit.LimitScope.SubCategory:
                    EditorGUILayout.PropertyField(subCategoryProp, new GUIContent("SubCategory"));
                    break;
                case CategoryLimit.LimitScope.ModuleType:
                    EditorGUILayout.PropertyField(moduleTypeProp, new GUIContent("Module Type"));
                    break;
            }

            EditorGUILayout.PropertyField(maxCountProp, new GUIContent("Max Count"));

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            return false;
        }

        private void AddNewCategoryLimit()
        {
            serializedObject.Update();

            int newIndex = _categoryLimits.arraySize;
            _categoryLimits.InsertArrayElementAtIndex(newIndex);

            var newLimit = _categoryLimits.GetArrayElementAtIndex(newIndex);
            newLimit.FindPropertyRelative("scope").enumValueIndex = (int)CategoryLimit.LimitScope.Category;
            newLimit.FindPropertyRelative("category").enumValueIndex = (int)ModuleCategory.Weapons;
            newLimit.FindPropertyRelative("maxCount").intValue = 2;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
