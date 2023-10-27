using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;

namespace Starfire.Generation
{
  public interface IChunk
  {
    int ChunkIndex { get; }
    string ChunkGroupIndex { get; }
    Vector2Int ChunkKey { get; }
  }

  public class Chunk : IChunk
  {
    public int ChunkIndex { get; private set; }
    public string ChunkGroupIndex { get; private set; }
    public Vector2Int ChunkKey { get; private set; }

    public Chunk(int chunkIndex, Vector2Int key)
    {
      ChunkIndex = chunkIndex;
      ChunkGroupIndex = ChunkUtils.GetChunkGroupIndex(key);
      ChunkKey = key;
    }

    public ChunkSerializable ToSerializable()
    {
      return new ChunkSerializable(this);
    }
  }
}
