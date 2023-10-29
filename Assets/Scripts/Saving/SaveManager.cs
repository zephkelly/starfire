using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Starfire.Generation;

namespace Starfire.IO
{
  public class SaveManager
  {
    Dictionary<Vector2Int, ChunkListSerializable> chunkGroups = new Dictionary<Vector2Int, ChunkListSerializable>();

    public void SerializeDict(Dictionary<long, Chunk> chunksToSave, string saveFileName)
    {
      foreach (Chunk chunk in chunksToSave.Values)
      {
        string path = Application.persistentDataPath + $"/universes/{saveFileName}/cells/cell{chunk.ChunkGroupIndex.x}_{chunk.ChunkGroupIndex.y}.json";

        if (chunkGroups.ContainsKey(chunk.ChunkGroupIndex))
        {
          UpdateOrAddChunkToDictionary(chunk);
          continue;
        }

        if (File.Exists(path))
        {
          string json = File.ReadAllText(path);
          chunkGroups[chunk.ChunkGroupIndex] = JsonUtility.FromJson<ChunkListSerializable>(json);
          
          UpdateOrAddChunkToDictionary(chunk);
          continue;
        }
      
        chunkGroups[chunk.ChunkGroupIndex] = new ChunkListSerializable();
        chunkGroups[chunk.ChunkGroupIndex].Add(chunk.ToSerializable());
      }

      foreach (var chunkGroup in chunkGroups)
      {
        string path = Application.persistentDataPath + $"/universes/{saveFileName}/cells/cell{chunkGroup.Key.x}_{chunkGroup.Key.y}.json";
        string json = JsonUtility.ToJson(chunkGroup.Value);

        File.WriteAllText(path, json);
      }
    }

    public static Dictionary<long, Chunk> DeserializeDict(Vector2Int groupKey, string saveFileName)
    {
      Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>();

      string path = Application.persistentDataPath + $"/universes/{saveFileName}/cells/cell{groupKey.x}_{groupKey.y}.json";

      if (!Directory.Exists(path))
      {
        Debug.LogError("No chunks found.");
        return chunks;
      }

      string json = File.ReadAllText(path);
      ChunkListSerializable chunkSerializableList = JsonUtility.FromJson<ChunkListSerializable>(json);

      foreach (var chunk in chunkSerializableList.ToChunkList())
      {
        chunks[chunk.ChunkIndex] = chunk;
      }

      return chunks;
    }

    private void UpdateOrAddChunkToDictionary(Chunk chunk)
    {
      int index = chunkGroups[chunk.ChunkGroupIndex].ToChunkList().FindIndex(loadedChunk => loadedChunk.ChunkIndex == chunk.ChunkIndex);

      if (index != -1)
      {
        chunkGroups[chunk.ChunkGroupIndex].chunks[index] = chunk.ToSerializable();
      }
      else
      {
        chunkGroups[chunk.ChunkGroupIndex].Add(chunk.ToSerializable());
      }
    }

    Dictionary<Vector2Int, List<Chunk>> chunkGroups2 = new Dictionary<Vector2Int, List<Chunk>>();

    public void SerializeList(List<Chunk> chunksToSave)
    {
      CheckDirectoriesExist("zephyverse");

      foreach (Chunk chunk in chunksToSave)
      {
        string filePath = Application.persistentDataPath + $"/universes/zephyverse/cellsList/cell{chunk.ChunkGroupIndex}.json";

        if (!chunkGroups2.ContainsKey(chunk.ChunkGroupIndex))
        {
          chunkGroups2[chunk.ChunkGroupIndex] = LoadChunksFromFile(filePath);
        }

        UpdateOrAddChunkToList(chunkGroups2[chunk.ChunkGroupIndex], chunk);
      }

      foreach (var group in chunkGroups2)
      {
        string filePath = Application.persistentDataPath + $"/universes/zephyverse/cellsList/cell{group.Key}.json";
        SaveChunkGroupToFile(group.Value, filePath);
      }
    }

    private List<Chunk> LoadChunksFromFile(string filePath)
    {
      if (File.Exists(filePath))
      {
        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<ChunkListSerializable>(json).ToChunkList();
      }
      return new List<Chunk>();
    }

    private void UpdateOrAddChunkToList(List<Chunk> chunks, Chunk chunk)
    {
      int index = chunks.FindIndex(loadedChunk => loadedChunk.ChunkIndex == chunk.ChunkIndex);

      if (index != -1)
      {
        chunks[index] = chunk;
      }
      else
      {
        chunks.Add(chunk);
      }
    }

    public void CheckDirectoriesExist(string directoryName)
    {
      string directoryPath = Application.persistentDataPath + $"/universes/{directoryName}/cells";

      if (!Directory.Exists(directoryPath))
      {
        Directory.CreateDirectory(directoryPath);
      }
    }

    private void SaveChunkGroupToFile(List<Chunk> chunks, string filePath)
    {
      ChunkListSerializable chunkList = new ChunkListSerializable(chunks);

      string json = JsonUtility.ToJson(chunkList);
      File.WriteAllText(filePath, json);
    }
  }
}