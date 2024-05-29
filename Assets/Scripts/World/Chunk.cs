using System.Collections.Generic;
using UnityEngine;
using Starfire.IO;
using Unity.VisualScripting;

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

    // bool IsModified { get; }

    void SetActiveChunk(Vector2 chunkPosition, Vector2 chunkKey);
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
    // private Vector2 chunkWorldPosition;
    private bool hasChunkObject = false;
    private GameObject chunkObject = null;
    private const int chunkDiameter = 600;
    private ChunkState chunkState = ChunkState.Inactive;

    // Star info
    private bool hasStar = false;
    [SerializeField] private Vector2 starPosition;
    private GameObject starObject = null;
    private bool hasStarObject = false;
    private string starName;


    public long ChunkIndex { get => chunkIndex; }
    public Vector2Int ChunkKey { get => chunkKey; }
    public Vector2Int ChunkCellKey { get => chunkCellKey; }
    public ChunkState ChunkState { get => chunkState; }
    public GameObject ChunkObject { get => chunkObject; }
    public bool HasChunkObject { get => hasChunkObject; }

    public bool HasStar { get => hasStar; }
    public Vector2 StarPosition { get => starPosition; }
    public GameObject StarObject { get => starObject; }
    public bool HasStarObject { get => hasStarObject; }

    public Chunk(long _chunkIndex, Vector2Int _chunkKey, bool makeStar = false, bool preventMakeStar = false)
    {
      chunkIndex = _chunkIndex;
      chunkKey = _chunkKey;
    //   chunkWorldPosition = _worldPosition;
      chunkCellKey = ChunkUtils.GetChunkCell(chunkKey);

      hasStar = StarGenerator.Instance.ShouldSpawnStar(chunkKey, makeStar, preventMakeStar);

      if (hasStar)
      {
        // starPosition = StarGenerator.Instance.GetStarPosition(chunkDiameter) + (chunkWorldPosition * chunkDiameter);
      }
    }

    // public bool IsModified { get => isModified; }

    public void SetActiveChunk(Vector2 playerCurrentChunkPosition, Vector2 chunkKey)
    {
        if (chunkState == ChunkState.Active) return;
        chunkState = ChunkState.Active;

        SetChunkObject(playerCurrentChunkPosition, chunkKey);
        SetStarObject();
    }

    public void SetLazyChunk()
    {
        if (chunkState == ChunkState.Lazy) return;
        chunkState = ChunkState.Lazy;

        RemoveChunkObject();
        RemoveStarObject();
    }

    public void SetInactiveChunk()
    {
        if (chunkState == ChunkState.Inactive) return;
        chunkState = ChunkState.Inactive;

        RemoveChunkObject();
        RemoveStarObject();
    }

    private void SetChunkObject(Vector2 chunkPos, Vector2 chunkKey)
    {
      if (!hasChunkObject)
      {
        chunkObject = ChunkManager.Instance.ChunkPool.Get();
        chunkObject.name = $"Chunk{chunkIndex}";
        chunkObject.transform.position = new Vector2(
                chunkPos.x + (chunkKey.x * chunkDiameter),
                chunkPos.y + (chunkKey.y * chunkDiameter)
            );
        chunkObject.transform.SetParent(ChunkManager.Instance.transform);

        //create a new box colllider with is trigger true and size size of the diameter and place it in world position
        BoxCollider2D boxCollider = chunkObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(chunkDiameter, chunkDiameter);
        boxCollider.offset = new Vector2(chunkDiameter / 2, chunkDiameter / 2);
        boxCollider.isTrigger = true;

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

        NameGenerator nameGenerator = new NameGenerator();
        var starName = nameGenerator.GetStarName();

        starObject.GetComponent<CelestialBehaviour>().SetupCelestialBehaviour(CelestialBodyType.Star, starName);
        
        starObject.transform.position = starPosition;
        starObject.transform.SetParent(chunkObject.transform);

        CameraController.Instance.starParallaxLayers.Add(starObject.GetComponent<StarParallaxLayer>());

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