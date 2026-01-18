using UnityEditor;
using UnityEngine;

namespace StarfireV2.Editor
{
    [CustomPropertyDrawer(typeof(ModuleSlotConfiguration))]
    public class ModuleSlotConfigurationDrawer : PropertyDrawer
    {
        private const float COLOR_INDICATOR_WIDTH = 6f;
        private const float REQ_BADGE_WIDTH = 32f;
        private const float TYPE_BUTTON_WIDTH = 160f;
        private const float SPACING = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return lineHeight + (lineHeight + spacing) * 5 + spacing * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var slotIdProp = property.FindPropertyRelative("slotId");
            var typeIdProp = property.FindPropertyRelative("typeId");
            var isRequiredProp = property.FindPropertyRelative("isRequired");
            var isAvailableProp = property.FindPropertyRelative("isAvailable");
            var defaultModuleProp = property.FindPropertyRelative("defaultModule");

            var typeId = (ShipModuleTypeId)typeIdProp.intValue;
            var category = ModuleEditorUtility.GetCategory(typeId);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect headerRect = new Rect(position.x, position.y, position.width, lineHeight);
            DrawHeader(headerRect, property, slotIdProp.stringValue, typeId, category, isRequiredProp.boolValue);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + lineHeight + spacing * 2;
                float fieldHeight = lineHeight;
                Rect fieldRect = new Rect(position.x, y, position.width, fieldHeight);

                EditorGUI.PropertyField(fieldRect, slotIdProp, new GUIContent("Slot ID"));
                fieldRect.y += fieldHeight + spacing;

                DrawTypeField(fieldRect, typeIdProp);
                fieldRect.y += fieldHeight + spacing;

                EditorGUI.PropertyField(fieldRect, isRequiredProp, new GUIContent("Required"));
                fieldRect.y += fieldHeight + spacing;

                EditorGUI.PropertyField(fieldRect, isAvailableProp, new GUIContent("Available"));
                fieldRect.y += fieldHeight + spacing;

                EditorGUI.PropertyField(fieldRect, defaultModuleProp, new GUIContent("Default Module"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private void DrawHeader(Rect rect, SerializedProperty property, string slotId,
            ShipModuleTypeId typeId, ShipModuleCategory category, bool isRequired)
        {
            float x = rect.x;

            Rect colorRect = new Rect(x, rect.y, COLOR_INDICATOR_WIDTH, rect.height);
            ModuleEditorUtility.DrawCategoryColorIndicator(colorRect, category);
            x += COLOR_INDICATOR_WIDTH + SPACING;

            float foldoutWidth = 14f;
            Rect foldoutRect = new Rect(x, rect.y, foldoutWidth, rect.height);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
            x += foldoutWidth;

            string displaySlotId = string.IsNullOrEmpty(slotId) ? "(unnamed)" : slotId;
            float remainingWidth = rect.width - (x - rect.x) - TYPE_BUTTON_WIDTH - SPACING;
            if (isRequired)
                remainingWidth -= REQ_BADGE_WIDTH + SPACING;

            Rect slotIdRect = new Rect(x, rect.y, remainingWidth, rect.height);
            EditorGUI.LabelField(slotIdRect, displaySlotId, EditorStyles.boldLabel);
            x += remainingWidth + SPACING;

            Rect typeRect = new Rect(x, rect.y, TYPE_BUTTON_WIDTH, rect.height);
            string typePath = ModuleEditorUtility.GetCategoryTypePath(typeId);
            var style = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10
            };
            EditorGUI.LabelField(typeRect, typePath, style);
            x += TYPE_BUTTON_WIDTH + SPACING;

            if (isRequired)
            {
                Rect reqRect = new Rect(x, rect.y, REQ_BADGE_WIDTH, rect.height);
                ModuleEditorUtility.DrawRequiredBadge(reqRect);
            }
        }

        private void DrawTypeField(Rect rect, SerializedProperty typeIdProp)
        {
            var currentType = (ShipModuleTypeId)typeIdProp.intValue;
            string displayPath = ModuleEditorUtility.GetCategoryTypePath(currentType);

            Rect labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            EditorGUI.LabelField(labelRect, "Type");

            Rect buttonRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                rect.width - EditorGUIUtility.labelWidth, rect.height);

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(displayPath), FocusType.Keyboard))
            {
                var menu = ModuleEditorUtility.BuildTypeSelectionMenu(selectedType =>
                {
                    typeIdProp.intValue = (int)selectedType;
                    typeIdProp.serializedObject.ApplyModifiedProperties();
                });
                menu.ShowAsContext();
            }
        }
    }
}
