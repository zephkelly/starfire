using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Starfire.Generation;
using System.Xml.Serialization;
using Unity.VisualScripting;

namespace Starfire.IO
{
  public class SaveManager
  {
    public static SaveManager Instance { get; private set; }

    Dictionary<Vector2Int, ChunkListSerializable> chunkCells = new Dictionary<Vector2Int, ChunkListSerializable>();

    private static string directoryName = "zephyverse";
    private static string path = Application.persistentDataPath + $"/universes/{directoryName}/";

    public SaveManager(string _directoryName)
    {
      if (Instance == null)
      {
        Instance = this;
      }
      else
      {
        Debug.LogError("SaveManager already exists.");
        return;
      }

      directoryName = _directoryName;
      CheckDirectoriesExist();
    }

    public void CheckDirectoriesExist()
    {
      if (!Directory.Exists(path + "cells"))
      {
        Directory.CreateDirectory(path + "cells");
      }
    }

    public IEnumerator SerializeChunks(Dictionary<Vector2Int, Chunk> chunksToSave)
    {
      int half = chunksToSave.Count / 2;
      int processed = 0;

      foreach (Chunk chunk in chunksToSave.Values)
      {
        if (chunkCells.ContainsKey(chunk.GetChunkCellKey()))
        {
          UpdateOrAddChunkToDictionary(chunk);
          continue;
        }

        if (File.Exists(path + $"cells/cell{chunk.GetChunkCellKey()}.json"))
        {
          string json = File.ReadAllText(path + $"cells/cell{chunk.GetChunkCellKey()}.json");

          chunkCells[chunk.GetChunkCellKey()] = JsonUtility.FromJson<ChunkListSerializable>(json);
          chunkCells[chunk.GetChunkCellKey()].ListToDictionary();

          UpdateOrAddChunkToDictionary(chunk);
          continue;
        }

      
        chunkCells[chunk.GetChunkCellKey()] = new ChunkListSerializable();
        chunkCells[chunk.GetChunkCellKey()].AddChunk(chunk);

        if (++processed == half)
        {
            yield return null; // Wait for the next frame
        }
      }

      List<(string path, string json)> writeOperations = new List<(string, string)>();

      foreach (var chunkGroup in chunkCells)
      {
        chunkGroup.Value.DictionaryToList();
        string json = JsonUtility.ToJson(chunkGroup.Value);

        writeOperations.Add((path + $"/cells/cell{chunkGroup.Key}.json", json));
      }

      half = writeOperations.Count / 2;
      processed = 0;

      foreach (var writeOperation in writeOperations)
      {
        File.WriteAllText(writeOperation.path, writeOperation.json);

        if (++processed == half)
        {
            yield return null; // Wait for the next frame
        }
      }

    }

    public bool DoesCellFileExist(Vector2Int groupKey)
    {
      return File.Exists(path + $"cells/cell{groupKey}.json");
    }

    public ChunkListSerializable DeserializeChunkCell(Vector2Int groupKey, bool preCheckedForFile = false)
    {
      ChunkListSerializable loadedChunkCell = new ChunkListSerializable();

      if (!Directory.Exists(path))
      {
        Debug.LogError("No chunks found.");
        return loadedChunkCell;
      }

      if (!preCheckedForFile)
      {
        if (!File.Exists(path + $"cells/cell{groupKey}.json"))
        {
          Debug.LogError($"No chunk cell file found at {path + $"cells/cell{groupKey}.json"}.");
          return loadedChunkCell;
        }
      }

      string json = File.ReadAllText(path + $"/cells/cell{groupKey}.json");

      loadedChunkCell = JsonUtility.FromJson<ChunkListSerializable>(json);
      loadedChunkCell.ListToDictionary();

      return loadedChunkCell;
    }

    private void UpdateOrAddChunkToDictionary(Chunk chunk)
    {
      if (chunkCells[chunk.GetChunkCellKey()].chunksDict.ContainsKey(chunk.ChunkKey))
      {
        chunkCells[chunk.GetChunkCellKey()].chunksDict[chunk.ChunkKey] = chunk;
      }
      else
      {
        chunkCells[chunk.GetChunkCellKey()].AddChunk(chunk);
      }
    }
  }
}