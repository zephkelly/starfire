using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace StarfireV2.Editor
{
    public static class ModuleEditorUtility
    {
        private static readonly Dictionary<ShipModuleCategory, Color> _categoryColors = new()
        {
            { ShipModuleCategory.Structure, new Color(0.6f, 0.6f, 0.8f) },
            { ShipModuleCategory.Defense, new Color(0.4f, 0.7f, 0.9f) },
            { ShipModuleCategory.Offense, new Color(0.9f, 0.4f, 0.4f) },
            { ShipModuleCategory.Propulsion, new Color(0.5f, 0.8f, 0.5f) },
            { ShipModuleCategory.Utility, new Color(0.8f, 0.8f, 0.5f) },
            { ShipModuleCategory.Sensor, new Color(0.7f, 0.5f, 0.8f) },
            { ShipModuleCategory.Comms, new Color(0.5f, 0.7f, 0.7f) }
        };

        private static readonly Dictionary<ShipModuleTypeId, string> _displayNameCache = new();

        public static Color GetCategoryColor(ShipModuleCategory category)
        {
            return _categoryColors.TryGetValue(category, out var color) ? color : Color.gray;
        }

        public static string GetDisplayName(ShipModuleTypeId typeId)
        {
            if (_displayNameCache.TryGetValue(typeId, out var cached))
                return cached;

            string name = typeId.ToString();
            string displayName = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
            _displayNameCache[typeId] = displayName;
            return displayName;
        }

        public static ShipModuleCategory GetCategory(ShipModuleTypeId typeId)
        {
            return EntityModuleHierarchyRegistry.GetCategory(typeId);
        }

        public static string GetCategoryTypePath(ShipModuleTypeId typeId)
        {
            var category = GetCategory(typeId);
            return $"{category}/{GetDisplayName(typeId)}";
        }

        public static GenericMenu BuildTypeSelectionMenu(Action<ShipModuleTypeId> onSelect)
        {
            var menu = new GenericMenu();

            foreach (ShipModuleCategory category in Enum.GetValues(typeof(ShipModuleCategory)))
            {
                foreach (var typeId in EntityModuleHierarchyRegistry.GetTypesInCategory(category))
                {
                    var capturedType = typeId;
                    string displayName = GetDisplayName(typeId);
                    string path = $"{category}/{displayName}";

                    menu.AddItem(
                        new GUIContent(path),
                        false,
                        () => onSelect?.Invoke(capturedType)
                    );
                }
            }

            return menu;
        }

        public static void DrawCategoryColorIndicator(Rect position, ShipModuleCategory category)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = GetCategoryColor(category);
            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);
            GUI.backgroundColor = oldColor;
        }

        public static void DrawRequiredBadge(Rect position)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            GUI.Label(position, "REQ", EditorStyles.miniButton);
            GUI.backgroundColor = oldColor;
        }
    }
}
