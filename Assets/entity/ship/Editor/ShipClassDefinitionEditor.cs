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
        private SerializedProperty _classId;
        private SerializedProperty _className;
        private SerializedProperty _description;
        private SerializedProperty _classIcon;
        private SerializedProperty _moduleSlots;
        private SerializedProperty _capabilities;
        private SerializedProperty _prefabOverride;
        private SerializedProperty _spriteScale;

        private bool _identityFoldout = true;
        private bool _slotsFoldout = true;
        private bool _capabilitiesFoldout = true;
        private bool _visualFoldout = true;

        private readonly Dictionary<int, bool> _slotExpanded = new();

        private void OnEnable()
        {
            _classId = serializedObject.FindProperty("classId");
            _className = serializedObject.FindProperty("className");
            _description = serializedObject.FindProperty("description");
            _classIcon = serializedObject.FindProperty("classIcon");
            _moduleSlots = serializedObject.FindProperty("moduleSlots");
            _capabilities = serializedObject.FindProperty("capabilities");
            _prefabOverride = serializedObject.FindProperty("prefabOverride");
            _spriteScale = serializedObject.FindProperty("spriteScale");
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
            _slotsFoldout = EditorGUILayout.Foldout(_slotsFoldout, "Module Slots", true, EditorStyles.foldoutHeader);
            if (_slotsFoldout)
            {
                DrawModuleSlotsSection();
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

        private void DrawModuleSlotsSection()
        {
            EditorGUI.indentLevel++;

            int slotCount = _moduleSlots.arraySize;
            EditorGUILayout.LabelField($"Configured Slots: {slotCount}", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // Draw existing slots
            for (int i = 0; i < _moduleSlots.arraySize; i++)
            {
                if (DrawSlotEntry(i))
                {
                    // Slot was deleted, break to avoid index issues
                    break;
                }
            }

            EditorGUILayout.Space(10);

            // Add slot button
            DrawAddSlotButton();

            EditorGUI.indentLevel--;
        }

        private bool DrawSlotEntry(int index)
        {
            var slotProp = _moduleSlots.GetArrayElementAtIndex(index);
            var slotTypeProp = slotProp.FindPropertyRelative("slotType");
            var isRequiredProp = slotProp.FindPropertyRelative("isRequired");
            var defaultModuleProp = slotProp.FindPropertyRelative("defaultModule");

            var slotType = (ModuleSlotType)slotTypeProp.enumValueIndex;
            string displayName = ModuleTypeRegistry.GetDisplayName(slotType);

            if (!_slotExpanded.ContainsKey(index))
                _slotExpanded[index] = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row
            EditorGUILayout.BeginHorizontal();

            _slotExpanded[index] = EditorGUILayout.Foldout(_slotExpanded[index], displayName, true, EditorStyles.foldoutHeader);

            // Required badge
            if (isRequiredProp.boolValue)
            {
                GUILayout.Label("REQ", EditorStyles.miniLabel, GUILayout.Width(30));
            }

            // Validation indicator
            var config = defaultModuleProp.objectReferenceValue as ScriptableObject;
            bool isValid = config == null || ModuleTypeRegistry.IsValidConfigForSlot(slotType, config);

            var oldColor = GUI.color;
            GUI.color = isValid ? Color.green : Color.red;
            GUILayout.Label(isValid ? "\u2713" : "\u2717", GUILayout.Width(20));
            GUI.color = oldColor;

            // Remove button
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                _moduleSlots.DeleteArrayElementAtIndex(index);
                _slotExpanded.Remove(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true; // Signal deletion
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Expanded content
            if (_slotExpanded[index])
            {
                EditorGUI.indentLevel++;

                // Slot type (read-only)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(slotTypeProp);
                EditorGUI.EndDisabledGroup();

                // Required toggle
                EditorGUILayout.PropertyField(isRequiredProp, new GUIContent("Required"));

                // Type-filtered config field
                DrawTypedConfigField(slotType, defaultModuleProp);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return false;
        }

        private void DrawTypedConfigField(ModuleSlotType slotType, SerializedProperty configProp)
        {
            Type expectedType = ModuleTypeRegistry.GetConfigTypeForSlot(slotType);
            string label = $"Default {ModuleTypeRegistry.GetDisplayName(slotType)}";

            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.ObjectField(
                label,
                configProp.objectReferenceValue,
                expectedType,
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                configProp.objectReferenceValue = newValue;
            }

            // Validation warning
            var current = configProp.objectReferenceValue as ScriptableObject;
            if (current != null && !ModuleTypeRegistry.IsValidConfigForSlot(slotType, current))
            {
                EditorGUILayout.HelpBox(
                    $"Type mismatch: Expected {expectedType?.Name}, got {current.GetType().Name}",
                    MessageType.Error
                );
            }
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

        private void DrawAddSlotButton()
        {
            // Get used slot types
            var usedTypes = new HashSet<ModuleSlotType>();
            for (int i = 0; i < _moduleSlots.arraySize; i++)
            {
                var slotTypeProp = _moduleSlots.GetArrayElementAtIndex(i).FindPropertyRelative("slotType");
                usedTypes.Add((ModuleSlotType)slotTypeProp.enumValueIndex);
            }

            // Get available types
            var availableTypes = Enum.GetValues(typeof(ModuleSlotType))
                .Cast<ModuleSlotType>()
                .Where(t => !usedTypes.Contains(t))
                .ToList();

            if (availableTypes.Count == 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("All Module Types Added");
                EditorGUI.EndDisabledGroup();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("+ Add Module Slot", GUILayout.Width(150)))
            {
                ShowAddSlotMenu(availableTypes);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddSlotMenu(List<ModuleSlotType> availableTypes)
        {
            var menu = new GenericMenu();

            // Categories
            var coreModules = new[] { ModuleSlotType.Hull, ModuleSlotType.Shield, ModuleSlotType.Deflector };
            var movementModules = new[] { ModuleSlotType.Propulsion, ModuleSlotType.Rotation, ModuleSlotType.WarpDrive, ModuleSlotType.Hyperdrive };
            var systemModules = new[] { ModuleSlotType.Weapon, ModuleSlotType.Sensor, ModuleSlotType.Transponder };
            var utilityModules = new[] { ModuleSlotType.CargoBay, ModuleSlotType.AICore, ModuleSlotType.LifeSupport };

            AddMenuCategory(menu, "Core/", coreModules, availableTypes);
            AddMenuCategory(menu, "Movement/", movementModules, availableTypes);
            AddMenuCategory(menu, "Systems/", systemModules, availableTypes);
            AddMenuCategory(menu, "Utility/", utilityModules, availableTypes);

            menu.ShowAsContext();
        }

        private void AddMenuCategory(GenericMenu menu, string prefix, ModuleSlotType[] categoryTypes, List<ModuleSlotType> availableTypes)
        {
            foreach (var slotType in categoryTypes)
            {
                if (!availableTypes.Contains(slotType)) continue;

                var type = slotType;
                menu.AddItem(
                    new GUIContent($"{prefix}{ModuleTypeRegistry.GetDisplayName(type)}"),
                    false,
                    () => AddNewSlot(type)
                );
            }
        }

        private void AddNewSlot(ModuleSlotType slotType)
        {
            serializedObject.Update();

            int newIndex = _moduleSlots.arraySize;
            _moduleSlots.InsertArrayElementAtIndex(newIndex);

            var newSlot = _moduleSlots.GetArrayElementAtIndex(newIndex);
            newSlot.FindPropertyRelative("slotType").enumValueIndex = (int)slotType;
            newSlot.FindPropertyRelative("isRequired").boolValue = false;
            newSlot.FindPropertyRelative("defaultModule").objectReferenceValue = null;

            _slotExpanded[newIndex] = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
