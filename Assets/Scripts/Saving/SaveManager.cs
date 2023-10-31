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
    Dictionary<Vector2Int, ChunkListSerializable> chunkCells = new Dictionary<Vector2Int, ChunkListSerializable>();

    private static string directoryName = "zephyverse";
    private static string path = Application.persistentDataPath + $"/universes/{directoryName}/";

    public SaveManager(string _directoryName)
    {
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

    public void SerializeDict(Dictionary<long, Chunk> chunksToSave)
    {
      foreach (Chunk chunk in chunksToSave.Values)
      {
        if (chunkCells.ContainsKey(chunk.ChunkCellKey))
        {
          UpdateOrAddChunkToDictionary(chunk);
          continue;
        }

        if (File.Exists(path))
        {
          string json = File.ReadAllText(path + $"cells/cell{chunk.ChunkCellKey}.json");

          chunkCells[chunk.ChunkCellKey] = JsonUtility.FromJson<ChunkListSerializable>(json);
          chunkCells[chunk.ChunkCellKey].ListToDictionary();
          
          UpdateOrAddChunkToDictionary(chunk);
          continue;
        }
      
        chunkCells[chunk.ChunkCellKey] = new ChunkListSerializable();
        chunkCells[chunk.ChunkCellKey].AddChunk(chunk);
      }

      List<(string path, string json)> writeOperations = new List<(string, string)>();

      foreach (var chunkGroup in chunkCells)
      {
        chunkGroup.Value.DictionaryToList();
        string json = JsonUtility.ToJson(chunkGroup.Value);

        writeOperations.Add((path + $"/cells/cell{chunkGroup.Key}.json", json));
      }

      foreach (var writeOperation in writeOperations)
      {
        File.WriteAllText(writeOperation.path, writeOperation.json);
      }
    }

    // public static Dictionary<long, Chunk> DeserializeDict(Vector2Int groupKey)
    // {
    //   Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>();

    //   string path = Application.persistentDataPath + $"/universes/{saveFileName}/cells/cell{groupKey.x}_{groupKey.y}.json";

    //   if (!Directory.Exists(path))
    //   {
    //     Debug.LogError("No chunks found.");
    //     return chunks;
    //   }

    //   string json = File.ReadAllText(path);
    //   ChunkListSerializable chunkSerializableList = JsonUtility.FromJson<ChunkListSerializable>(json);

    //   foreach (var chunk in chunkSerializableList.ToChunkList())
    //   {
    //     chunks[chunk.ChunkIndex] = chunk;
    //   }

    //   return chunks;
    // }

    private void UpdateOrAddChunkToDictionary(Chunk chunk)
    {
      if (chunkCells[chunk.ChunkCellKey].chunksDict.ContainsKey(chunk.ChunkIndex))
      {
        chunkCells[chunk.ChunkCellKey].chunksDict[chunk.ChunkIndex] = chunk;
      }
      else
      {
        chunkCells[chunk.ChunkCellKey].AddChunk(chunk);
      }
    }
  }
}