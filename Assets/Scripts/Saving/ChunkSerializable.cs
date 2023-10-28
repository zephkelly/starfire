using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Generation;

namespace Starfire.IO
{
  // This is a serializable form of the Chunk class
  [System.Serializable]
  public class ChunkSerializable
  {
    public long chunkIndex;
    public int x;
    public int y;

    public ChunkSerializable(Chunk chunk)
    {
      chunkIndex = chunk.ChunkIndex;
      x = chunk.ChunkKey.x;
      y = chunk.ChunkKey.y;
    }

    public Vector2Int ChunkKey
    {
      get => new Vector2Int(x, y);
    }

    public Chunk ToChunk()
    {
      return new Chunk(chunkIndex, new Vector2Int(x, y));
    }
  }

  [System.Serializable]
  public class ChunkListSerializable
  {
    public List<ChunkSerializable> chunks = new List<ChunkSerializable>();

    public ChunkListSerializable(List<Chunk> inactiveChunks)
    {
      foreach (Chunk chunk in inactiveChunks)
      {
        chunks.Add(chunk.ToSerializable());
      }
    }

    public List<Chunk> GetChunkList()
    {
      List<Chunk> loadedChunks = new List<Chunk>();

      foreach (ChunkSerializable chunk in chunks)
      {
        loadedChunks.Add(chunk.ToChunk());
      }

      return loadedChunks;
    }
  }
}