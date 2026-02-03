using UnityEngine;

namespace Starfire.Core.V2.Save.Config
{
    public enum SaveFormat
    {
        Binary,
        Json
    }

    [CreateAssetMenu(fileName = "SaveSystemConfig", menuName = "Starfire/Save/Save System Config")]
    public class SaveSystemConfig : ScriptableObject
    {
        [Header("Serialization")]
        [Tooltip("Binary is compact and fast. JSON is human-readable for debugging.")]
        public SaveFormat format = SaveFormat.Binary;

        [Tooltip("Pretty-print JSON output (only applies to JSON format)")]
        public bool jsonPrettyPrint = true;

        [Header("Save Slots")]
        [Tooltip("Maximum number of manual save slots")]
        [Range(1, 20)]
        public int maxSaveSlots = 5;

        [Header("Auto-Save")]
        [Tooltip("Enable automatic saving at regular intervals")]
        public bool autoSaveEnabled = true;

        [Tooltip("Seconds between auto-saves")]
        [Range(30f, 600f)]
        public float autoSaveIntervalSeconds = 300f;

        [Tooltip("Number of rotating auto-save files to keep")]
        [Range(1, 10)]
        public int maxAutoSaves = 3;

        [Header("Performance")]
        [Tooltip("Entities to spawn per frame during load")]
        [Range(10, 200)]
        public int entitiesPerFrame = 50;

        [Tooltip("Chunk modifications to apply per frame during load")]
        [Range(1, 20)]
        public int chunkModificationsPerFrame = 5;

        [Header("File Settings")]
        [Tooltip("Subdirectory under Application.persistentDataPath")]
        public string saveDirectory = "saves";

        [Tooltip("File extension for save files")]
        public string fileExtension = "sav";
    }
}
