// using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;
using System.Linq;

namespace Starfire.Generation
{
  public interface IChunk
  {
    long ChunkIndex { get; }
    Vector2Int ChunkCellKey { get; }
    Vector2Int ChunkKey { get; }
    Vector2 AbsolutePosition { get; }
    Vector2 WorldPosition { get; }
    bool HasSetChunkObject { get; }

    Vector2 StarPosition { get; }
    bool HasStar { get; }
    void SetStar(GameObject _starObject);
  }

  [System.Serializable]
  public class Chunk : IChunk
  {
    public Vector2Int chunkKey;
    public Vector2 starPosition;
    public bool hasStar = false;
    
    private const int chunkDiameter = 300;
    private GameObject chunkObject;
    private GameObject starObject;

    public long ChunkIndex { get; private set; }
    public Vector2Int ChunkKey { get => chunkKey; }
    public Vector2Int ChunkCellKey { get; private set; }
    public Vector2 AbsolutePosition { get => chunkKey * chunkDiameter; }
    public Vector2 WorldPosition { get; private set; }

    public Vector2 StarPosition { get => starPosition; }
    public bool HasSetChunkObject { get; private set; }
    public bool HasStar { get => hasStar; }

    public Vector2Int GetChunkCellKey()
    {
      ChunkCellKey = ChunkUtils.GetChunkGroup(chunkKey);

      return ChunkCellKey;
    }

    public Chunk(long _chunkIndex, Vector2Int _chunkKey, Vector2 _chunkWorldPosition)
    {
      ChunkIndex = _chunkIndex;
      ChunkCellKey = ChunkUtils.GetChunkGroup(_chunkKey);
      chunkKey = _chunkKey;
      WorldPosition = _chunkWorldPosition;
    }

    public void SetStar(GameObject _starObject = null)
    {
      if (HasStar) 
      {
        starObject.SetActive(true);
        starObject.transform.position = WorldPosition + starPosition;
        return;
      }

      if (_starObject == null) return;
      starObject = _starObject;
      hasStar = true;

      starPosition = new Vector2(
        UnityEngine.Random.Range(-chunkDiameter / 3, chunkDiameter / 3), 
        UnityEngine.Random.Range(-chunkDiameter / 3, chunkDiameter / 3)
      );

      starObject.transform.position = starPosition + WorldPosition;
    }

    public void SetChunkObject(Vector2 _worldPosition)
    {
      if (HasSetChunkObject) 
      {
        WorldPosition = _worldPosition;
        chunkObject.transform.position = WorldPosition;

        chunkObject.SetActive(true);
        SetStar();
        return;
      }

      HasSetChunkObject = true;
      chunkObject = new GameObject("Chunk");
      chunkObject.transform.position = _worldPosition;

      //Box Collider to visualise chunk
      BoxCollider2D chunkCollider = chunkObject.AddComponent<BoxCollider2D>();
      chunkCollider.size = new Vector2(chunkDiameter, chunkDiameter);
      chunkCollider.isTrigger = true;
    }

    public void DestroyChunkObject()
    {
      if (chunkObject == null) return;
      UnityEngine.Object.Destroy(chunkObject);
    }

    public void DeactivateChunkObject()
    {
      if (HasSetChunkObject == false) return;
      chunkObject.SetActive(false);
      
      if (HasStar == false) return;
      starObject.SetActive(false);
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
