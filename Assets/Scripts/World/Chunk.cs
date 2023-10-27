using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;

namespace Starfire.Generation
{
  public interface IChunk
  {
    int Index { get; }
    Vector2Int ChunkKey { get; }
  }

  public class Chunk : IChunk
  {
    public int Index { get; private set; }
    public Vector2Int ChunkKey { get; private set; }

    public Chunk(int chunkIndex, Vector2Int key)
    {
      ChunkKey = key;
      Index = chunkIndex;
    }

    public ChunkSerializable ToSerializable()
    {
      return new ChunkSerializable(this);
    }
  }
}
