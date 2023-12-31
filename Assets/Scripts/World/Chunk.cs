using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;

namespace Starfire
{
  public interface IChunk
  {
    long ChunkIndex { get; }
    Vector2Int ChunkKey { get; }
    Vector2Int ChunkCellKey { get; }
    ChunkState ChunkState { get; }
    bool HasChunkObject { get; }
    GameObject ChunkObject { get; }

    bool HasStar { get; }
    Vector2 StarPosition { get; }
    bool HasStarObject { get; }
    GameObject StarObject { get; }

    bool IsModified { get; }

    void SetActiveChunk();
    void SetLazyChunk();
    void SetInactiveChunk();
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
    // Chunk info
    [SerializeField] private long chunkIndex;
    [SerializeField] private Vector2Int chunkKey;
    private Vector2Int chunkCellKey;
    private Vector2 chunkWorldPosition;
    private bool hasChunkObject = false;
    private GameObject chunkObject = null;
    private const int chunkDiameter = 300;
    private ChunkState chunkState = ChunkState.Inactive;

    // Star info
    [SerializeField] private bool hasStar = false;
    [SerializeField] private Vector2 starPosition;
    private bool hasStarObject = false;
    private GameObject starObject = null;

    private bool isModified = false;

    public Chunk(long _chunkIndex, Vector2Int _chunkKey, Vector2 _worldPosition)
    {
      chunkIndex = _chunkIndex;
      chunkKey = _chunkKey;
      chunkWorldPosition = _worldPosition;
      chunkCellKey = ChunkUtils.GetChunkCell(chunkKey);

      hasStar = StarGenerator.Instance.ShouldSpawnStar(chunkKey);

      if (hasStar)
      {
        starPosition = StarGenerator.Instance.GetStarPosition(chunkDiameter) + (chunkWorldPosition * chunkDiameter);
      }
    }

    public long ChunkIndex { get => chunkIndex; }
    public Vector2Int ChunkKey { get => chunkKey; }
    public Vector2Int ChunkCellKey { get => chunkCellKey; }
    public ChunkState ChunkState { get => chunkState; }
    public bool HasChunkObject { get => hasChunkObject; }
    public GameObject ChunkObject { get => chunkObject; }

    public bool HasStar { get => hasStar; }
    public Vector2 StarPosition { get => starPosition; }
    public bool HasStarObject { get => hasStarObject; }
    public GameObject StarObject { get => starObject; }

    public bool IsModified { get => isModified; }

    public void SetActiveChunk()
    {
      chunkState = ChunkState.Active;

      SetChunkObject();
      SetStarObject();
    }

    public void SetLazyChunk()
    {
      chunkState = ChunkState.Lazy;

      RemoveChunkObject();
      RemoveStarObject();
    }

    public void SetInactiveChunk()
    {
      chunkState = ChunkState.Inactive;

      RemoveChunkObject();
      RemoveStarObject();
    }

    private void SetChunkObject()
    {
      if (!hasChunkObject)
      {
        chunkObject = ChunkManager.Instance.ChunkPool.Get();
        chunkObject.name = $"Chunk{chunkIndex}";
        chunkObject.transform.position = chunkWorldPosition * chunkDiameter;

        hasChunkObject = true;
      }
    }

    private void RemoveChunkObject()
    {
      if (hasChunkObject)
      {
        ChunkManager.Instance.ChunkPool.Release(chunkObject);
        chunkObject = null;
        hasChunkObject = false;
      }
    }

    private void SetStarObject()
    {
      if (hasStar && hasStarObject == false)
      {
        starObject = StarGenerator.Instance.StarPool.Get();
        starObject.transform.position = starPosition;
        starObject.transform.SetParent(chunkObject.transform);

        hasStarObject = true;
      }
    }

    private void RemoveStarObject()
    {
      if (hasStar && hasStarObject)
      {
        StarGenerator.Instance.StarPool.Release(starObject);
        starObject = null;
        hasStarObject = false;
      }
    }
  }
}