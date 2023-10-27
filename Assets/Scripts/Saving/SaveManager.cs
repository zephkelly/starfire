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
    Dictionary<string, List<Chunk>> chunkGroups = new Dictionary<string, List<Chunk>>();

    public void SaveChunks(List<Chunk> chunksToSave)
    {
      foreach (Chunk chunk in chunksToSave)
      {
        string filePath = Application.persistentDataPath + $"/universes/zephyverse/cells/{chunk.ChunkGroupIndex}.json";

        if (!chunkGroups.ContainsKey(chunk.ChunkGroupIndex))
        {
          chunkGroups[chunk.ChunkGroupIndex] = LoadChunksFromFile(filePath);
        }

        UpdateOrAddChunkToList(chunkGroups[chunk.ChunkGroupIndex], chunk);
      }

      foreach (var group in chunkGroups)
      {
        string filePath = Application.persistentDataPath + $"/universes/zephyverse/cells/{group.Key}.json";
        SaveChunkGroupToFile(group.Value, filePath);
      }

      Debug.Log("Saved chunks to file.");
    }

    private List<Chunk> LoadChunksFromFile(string filePath)
    {
      if (File.Exists(filePath))
      {
        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<ChunkListSerializable>(json).GetChunkList();
      }
      return new List<Chunk>();
    }

    private void UpdateOrAddChunkToList(List<Chunk> chunks, Chunk chunk)
    {
      //Could implement binary search function to find index more efficiently
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

    private void SaveChunkGroupToFile(List<Chunk> chunks, string filePath)
    {
      ChunkListSerializable chunkList = new ChunkListSerializable(chunks);

      string json = JsonUtility.ToJson(chunkList);
      File.WriteAllText(filePath, json);
    }

    public void CheckDirectoriesExist(string directoryName)
    {
      string directoryPath = Application.persistentDataPath + $"/universes/{directoryName}/cells";

      if (!Directory.Exists(directoryPath))
      {
        Directory.CreateDirectory(directoryPath);
      }
    }
  }
}