using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;

namespace Starfire.Generation
{
  public interface IChunk
  {
    long ChunkIndex { get; }
    Vector2Int ChunkGroupIndex { get; }
    Vector2Int ChunkKey { get; }
  }

  public class Chunk : IChunk
  {
    public long ChunkIndex { get; private set; }
    public Vector2Int ChunkGroupIndex { get; private set; }
    public Vector2Int ChunkKey { get; private set; }

    public Chunk(long chunkIndex, Vector2Int key)
    {
      ChunkIndex = chunkIndex;
      ChunkGroupIndex = ChunkUtils.GetChunkGroup(key);
      ChunkKey = key;
    }

    public ChunkSerializable ToSerializable()
    {
      return new ChunkSerializable(this);
    }
  }
}
