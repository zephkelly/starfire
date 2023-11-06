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
    ChunkState ChunkState { get; }
    bool IsModified { get; }
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

  public enum ChunkState
  {
    Active,
    Lazy,
    Inactive
  }

  [System.Serializable]
  public class Chunk : IChunk
  {
    [SerializeField] private long chunkIndex;
    [SerializeField] private Vector2Int chunkKey;
    [SerializeField] private Vector2 starPosition;
    [SerializeField] public bool hasStar = false;

    private const int chunkDiameter = 300;
    private ChunkState chunkState = ChunkState.Inactive;
    private bool isModified = false;
    
    private GameObject chunkObject;
    private GameObject starObject;

    public ChunkState ChunkState { get => chunkState; }
    public bool IsModified { get => isModified; }

    public long ChunkIndex { get => chunkIndex; }
    public Vector2Int ChunkKey { get => chunkKey; }
    public Vector2Int ChunkCellKey { get; private set; }

    public Vector2 AbsolutePosition { get => chunkKey * chunkDiameter; }
    public Vector2 WorldPosition { get; private set; }

    public GameObject ChunkObject { get => chunkObject; }
    public bool HasSetChunkObject { get; private set; }

    public Vector2 StarPosition { get => starPosition; }
    public bool HasStar { get => hasStar; }

    public Chunk(long _chunkIndex, Vector2Int _chunkKey)
    {
      chunkIndex = _chunkIndex;
      chunkKey = _chunkKey;
      ChunkCellKey = GetChunkCellKey();
    }

    public Vector2Int GetChunkCellKey()
    {
      ChunkCellKey = ChunkUtils.GetChunkGroup(chunkKey);
      return ChunkCellKey;
    }

    public void ActiveChunk()
    {
      chunkState = ChunkState.Active;
    }

    public void LazyChunk()
    {
      chunkState = ChunkState.Lazy;
    }

    public void InactiveChunk()
    {
      chunkState = ChunkState.Inactive;
    }
  }
}