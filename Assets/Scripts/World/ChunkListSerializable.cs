using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Starfire
{
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