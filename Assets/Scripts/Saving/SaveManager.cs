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
    public void SaveChunks(List<Chunk> chunksToSave)
    {
      string path = Application.persistentDataPath + "/chunks.json";

      if (File.Exists(path))
      {
        string json = File.ReadAllText(path);

        List<Chunk> loadedChunks = JsonUtility.FromJson<ChunkListSerializable>(json).GetChunkList();
        // List<Chunk> chunksToUpdate = new List<Chunk>();

        foreach (Chunk chunk in chunksToSave)
        {
          int index = loadedChunks.FindIndex(loadedChunk => loadedChunk.Index == chunk.Index);

          if (index != -1)
          {
            loadedChunks[index] = chunk;
          }
          else
          {
            loadedChunks.Add(chunk);
          }
        }

        File.WriteAllText(path, "");

        ChunkListSerializable chunkList = new ChunkListSerializable(loadedChunks);
        json = JsonUtility.ToJson(chunkList);

        File.WriteAllText(path, json);
      }
      else
      {
        ChunkListSerializable chunkList = new ChunkListSerializable(chunksToSave);
        string json = JsonUtility.ToJson(chunkList);

        File.WriteAllText(path, json);
        Debug.Log("File saved. Chunk count: " + chunksToSave.Count);
      }
    }

    public List<Chunk> LoadChunks(List<Vector2Int> chunksToLoad)
    {

    }
  }
}