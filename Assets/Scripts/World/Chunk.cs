using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;
using Unity.VisualScripting;
using System.Linq;

namespace Starfire.Generation
{
  public interface IChunk
  {
    long ChunkIndex { get; }
    Vector2Int ChunkCellKey { get; }
    Vector2Int ChunkKey { get; }
  }

  [System.Serializable]
  public class Chunk : IChunk
  {
    public Vector2Int chunkKey;
    public List<long> randLongs = new List<long>();

    public long ChunkIndex { get; private set; }
    public Vector2Int ChunkKey { get => chunkKey; }
    public Vector2Int ChunkCellKey { get; private set; }

    public Vector2Int GetChunkCellKey()
    {
      ChunkCellKey = ChunkUtils.GetChunkGroup(chunkKey);

      return ChunkCellKey;
    }

    public Chunk(long _chunkIndex, Vector2Int _chunkKey)
    {
      ChunkIndex = _chunkIndex;
      ChunkCellKey = ChunkUtils.GetChunkGroup(_chunkKey);
      chunkKey = _chunkKey;

      for (int i = 0; i < 100; i++)
      {
        randLongs.Add((long)UnityEngine.Random.Range(0, 1000000000000000000));
      }
    }
  }

  [System.Serializable]
  public class ChunkListSerializable
  {
    public Dictionary<Vector2Int, Chunk> chunksDict = new Dictionary<Vector2Int, Chunk>();
    public List<Chunk> chunks = new List<Chunk>();

    public void AddChunk(Chunk newChunk)
    {
      chunksDict[newChunk.ChunkKey] = newChunk;
    }

    public Dictionary<Vector2Int, Chunk> ListToDictionary()
    {
      chunksDict = chunks.ToDictionary(chunk => chunk.ChunkKey);
      return chunksDict;
    }

    public List<Chunk> DictionaryToList()
    {
      chunks = chunksDict.Values.ToList();
      return chunks;
    }
  }
}
