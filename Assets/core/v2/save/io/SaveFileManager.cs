using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Starfire.Core.V2.Save.Config;
using Starfire.Core.V2.Save.Serialization;
using UnityEngine;

namespace Starfire.Core.V2.Save.IO
{
    public class SaveSlotInfo
    {
        public string SlotName;
        public string FilePath;
        public DateTime Timestamp;
        public string SaveName;
        public int Version;
        public bool IsAutoSave;
    }

    public class SaveFileManager
    {
        private readonly SaveSystemConfig _config;
        private readonly string _basePath;
        private int _autoSaveIndex;

        public SaveFileManager(SaveSystemConfig config)
        {
            _config = config;
            _basePath = Path.Combine(Application.persistentDataPath, config.saveDirectory);
            EnsureDirectories();
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(Path.Combine(_basePath, "slots"));
            Directory.CreateDirectory(Path.Combine(_basePath, "autosaves"));
            Directory.CreateDirectory(Path.Combine(_basePath, "quicksave"));
        }

        // ── Write ────────────────────────────────────────────────────────

        public async Task WriteSlotAsync(string slotName, byte[] data)
        {
            string dir = Path.Combine(_basePath, "slots", slotName);
            Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, $"save.{_config.fileExtension}");
            await WriteAtomicAsync(filePath, data);
        }

        public async Task WriteAutoSaveAsync(byte[] data)
        {
            string fileName = $"autosave_{_autoSaveIndex:D3}.{_config.fileExtension}";
            string filePath = Path.Combine(_basePath, "autosaves", fileName);
            await WriteAtomicAsync(filePath, data);
            _autoSaveIndex = (_autoSaveIndex + 1) % _config.maxAutoSaves;
        }

        public async Task WriteQuickSaveAsync(byte[] data)
        {
            string filePath = Path.Combine(_basePath, "quicksave", $"quicksave.{_config.fileExtension}");
            await WriteAtomicAsync(filePath, data);
        }

        private async Task WriteAtomicAsync(string filePath, byte[] data)
        {
            string tempPath = filePath + ".tmp";
            try
            {
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await fs.WriteAsync(data, 0, data.Length);
                    await fs.FlushAsync();
                }

                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Move(tempPath, filePath);
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                throw;
            }
        }

        // ── Read ─────────────────────────────────────────────────────────

        public async Task<byte[]> ReadSlotAsync(string slotName)
        {
            string filePath = Path.Combine(_basePath, "slots", slotName, $"save.{_config.fileExtension}");
            return await ReadFileAsync(filePath);
        }

        public async Task<byte[]> ReadQuickSaveAsync()
        {
            string filePath = Path.Combine(_basePath, "quicksave", $"quicksave.{_config.fileExtension}");
            return await ReadFileAsync(filePath);
        }

        public async Task<byte[]> ReadAutoSaveAsync(int index)
        {
            string fileName = $"autosave_{index:D3}.{_config.fileExtension}";
            string filePath = Path.Combine(_basePath, "autosaves", fileName);
            return await ReadFileAsync(filePath);
        }

        private async Task<byte[]> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Save file not found: {filePath}");

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var data = new byte[fs.Length];
            await fs.ReadAsync(data, 0, data.Length);
            return data;
        }

        // ── Slot Management ──────────────────────────────────────────────

        public List<SaveSlotInfo> GetAllSlots(ISaveSerializer headerReader)
        {
            var slots = new List<SaveSlotInfo>();

            // Manual saves
            string slotsDir = Path.Combine(_basePath, "slots");
            if (Directory.Exists(slotsDir))
            {
                foreach (var dir in Directory.GetDirectories(slotsDir))
                {
                    var info = TryGetSlotInfo(dir, headerReader, false);
                    if (info != null) slots.Add(info);
                }
            }

            // Auto-saves
            string autoDir = Path.Combine(_basePath, "autosaves");
            if (Directory.Exists(autoDir))
            {
                foreach (var file in Directory.GetFiles(autoDir, $"*.{_config.fileExtension}"))
                {
                    var info = TryGetSlotInfoFromFile(file, headerReader, true);
                    if (info != null) slots.Add(info);
                }
            }

            // Quick save
            string quickPath = Path.Combine(_basePath, "quicksave", $"quicksave.{_config.fileExtension}");
            if (File.Exists(quickPath))
            {
                var info = TryGetSlotInfoFromFile(quickPath, headerReader, false);
                if (info != null)
                {
                    info.SlotName = "quicksave";
                    slots.Add(info);
                }
            }

            return slots.OrderByDescending(s => s.Timestamp).ToList();
        }

        public bool SlotExists(string slotName)
        {
            string filePath = Path.Combine(_basePath, "slots", slotName, $"save.{_config.fileExtension}");
            return File.Exists(filePath);
        }

        public void DeleteSlot(string slotName)
        {
            string dir = Path.Combine(_basePath, "slots", slotName);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        private SaveSlotInfo TryGetSlotInfo(string directory, ISaveSerializer headerReader, bool isAutoSave)
        {
            string filePath = Directory.GetFiles(directory, $"*.{_config.fileExtension}").FirstOrDefault();
            if (filePath == null) return null;
            return TryGetSlotInfoFromFile(filePath, headerReader, isAutoSave);
        }

        private SaveSlotInfo TryGetSlotInfoFromFile(string filePath, ISaveSerializer headerReader, bool isAutoSave)
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var header = headerReader.DeserializeHeader(bytes);
                return new SaveSlotInfo
                {
                    SlotName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(filePath) ?? filePath),
                    FilePath = filePath,
                    Timestamp = new DateTime(header.TimestampTicks),
                    Version = header.Version,
                    IsAutoSave = isAutoSave
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
