using UnityEditor;
using UnityEngine;
using Starfire.Core.V2.Save;

namespace Starfire.Core.V2.Save.Editor
{
    public class SaveSystemDebugWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _jsonOutput = "";
        private string _slotName = "debug_test";

        [MenuItem("Starfire/Save System Debug")]
        public static void ShowWindow()
        {
            GetWindow<SaveSystemDebugWindow>("Save Debug");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            bool isPlaying = Application.isPlaying;
            bool hasInstance = isPlaying && SaveSystem.Instance != null;

            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use the save system debug tools.", MessageType.Info);
                return;
            }

            if (!hasInstance)
            {
                EditorGUILayout.HelpBox("SaveSystem instance not found. Make sure a GameObject with SaveSystem is in the scene.", MessageType.Warning);
                return;
            }

            // ── Snapshot to JSON ──
            EditorGUILayout.LabelField("JSON Snapshot", EditorStyles.boldLabel);

            if (GUILayout.Button("Collect Save Data → JSON", GUILayout.Height(30)))
            {
                _jsonOutput = SaveSystem.Instance.CollectSaveDataAsJson();
                Debug.Log($"[SaveDebug] Collected {_jsonOutput.Length} chars of JSON");
            }

            if (!string.IsNullOrEmpty(_jsonOutput))
            {
                EditorGUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy to Clipboard"))
                {
                    GUIUtility.systemCopyBuffer = _jsonOutput;
                    Debug.Log("[SaveDebug] JSON copied to clipboard");
                }
                if (GUILayout.Button("Save to File"))
                {
                    string path = EditorUtility.SaveFilePanel("Save JSON", Application.persistentDataPath, "save_debug", "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        System.IO.File.WriteAllText(path, _jsonOutput);
                        Debug.Log($"[SaveDebug] Saved to {path}");
                        EditorUtility.RevealInFinder(path);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
                EditorGUILayout.TextArea(_jsonOutput, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(8);

            // ── Save/Load to Slot ──
            EditorGUILayout.LabelField("Save/Load Slot", EditorStyles.boldLabel);
            _slotName = EditorGUILayout.TextField("Slot Name", _slotName);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save to Slot"))
            {
                _ = SaveSystem.Instance.SaveToSlotAsync(_slotName);
            }
            if (GUILayout.Button("Load from Slot"))
            {
                _ = SaveSystem.Instance.LoadFromSlotAsync(_slotName);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Quick Save"))
            {
                _ = SaveSystem.Instance.QuickSaveAsync();
            }
            if (GUILayout.Button("Quick Load"))
            {
                _ = SaveSystem.Instance.LoadQuickSaveAsync();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // ── Info ──
            EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Save Directory", Application.persistentDataPath);
            if (GUILayout.Button("Open Save Directory"))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            }
        }
    }
}
