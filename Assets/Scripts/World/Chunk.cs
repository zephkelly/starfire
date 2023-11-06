// using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;
using System.Linq;

namespace Starfire
{
  public interface IChunk
  {
    long ChunkIndex { get; }
    Vector2Int ChunkCellKey { get; }
    Vector2Int ChunkKey { get; }

    Vector2 AbsolutePosition { get; }
    Vector2 WorldPosition { get; }

    GameObject ChunkObject { get; }
    bool HasSetChunkObject { get; }

    Vector2 StarPosition { get; }
    bool HasStar { get; }
    void SetStar(GameObject _starObject, Vector2 _starPosition);
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
    public GameObject ChunkObject { get => chunkObject; }

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

    public void SetStar(GameObject _starObject, Vector2 _starPosition)
    {
      hasStar = true;
      starObject = _starObject;
      starPosition = _starPosition;

      starObject.transform.position = WorldPosition + starPosition;
      starObject.transform.SetParent(chunkObject.transform);
    }

    public void SetChunkObject(Vector2 _worldPosition)
    {
      if (HasSetChunkObject) 
      {
        chunkObject.SetActive(true);

        if (HasStar)
        {
          starObject = StarGenerator.Instance.GetStar;
          starObject.SetActive(true);
          starObject.transform.position = WorldPosition + starPosition;
        }
        return;
      }

      HasSetChunkObject = true;
      chunkObject = new GameObject("Chunk" + ChunkIndex);
      chunkObject.transform.position = _worldPosition;

      //Box Collider to visualise chunk
      BoxCollider2D chunkCollider = chunkObject.AddComponent<BoxCollider2D>();
      chunkCollider.size = new Vector2(chunkDiameter, chunkDiameter);
      chunkCollider.isTrigger = true;
    }

    // public void DestroyChunkObject()
    // {
    //   if (chunkObject == null) return;
    //   UnityEngine.Object.Destroy(chunkObject);
    // }

    public void DeactivateChunkObject()
    {
      if (HasStar)
      {
        StarGenerator.Instance.ReleaseStar(starObject);
      }

      if (chunkObject == null) return;
      chunkObject.SetActive(false);
    }
  }
}