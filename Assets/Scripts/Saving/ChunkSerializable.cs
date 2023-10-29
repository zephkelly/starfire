using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Generation;

namespace Starfire.IO
{
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

    public ChunkListSerializable(List<Chunk> serilizableChunks = null)
    {
      if (serilizableChunks == null) return;

      foreach (Chunk chunk in serilizableChunks)
      {
        chunks.Add(chunk.ToSerializable());
      }
    }

    public List<Chunk> ToChunkList()
    {
      List<Chunk> deserializedChunks = new List<Chunk>();

      foreach (ChunkSerializable chunk in chunks)
      {
        deserializedChunks.Add(chunk.ToChunk());
      }

      return deserializedChunks;
    }

    public void Add(ChunkSerializable chunk)
    {
      chunks.Add(chunk);
    }
  }
}