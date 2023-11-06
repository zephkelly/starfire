using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;

namespace Starfire
{
  //TODO: Combine chunks to only one Dictionary
  //TODO: Remove unnecessary edge conditions
  //TODO: Create universal GetChunk, ReleaseChunk methods to be called in the GetChunks method

  [RequireComponent(typeof(StarGenerator))]
  public class ChunkManager : MonoBehaviour
  {
    const int chunkDiameter = 300;
    private int maxOriginDistance = 3000;

    private StarGenerator starGenerator;

    private Transform cameraTransform;
    private Transform entityTransform;

    private Vector2 entityLastPosition;
    private Vector2D entityAbsolutePosition;
    private Vector2Int entityAbsoluteChunkPosition;
    private Vector2Int entityLastAbsoluteChunkPosition;
    private Vector2Int entityRelativeChunkPosition;
    private Vector2Int entityLastRelativeChunkPosition;

    private Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<Vector2Int, Chunk> lazyChunks = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<Vector2Int, Chunk> inactiveChunks = new Dictionary<Vector2Int, Chunk>();
    private long chunkIndex = 0;

    public long ChunkIndex { get => chunkIndex++; }
    public int ChunkDiameter { get => chunkDiameter; }
    public int MaxOriginDistance { get => maxOriginDistance; }

    public Dictionary<Vector2Int, Chunk> ActiveChunks { get => activeChunks; }
    public Dictionary<Vector2Int, Chunk> LazyChunks { get => lazyChunks; }
    public Dictionary<Vector2Int, Chunk> InactiveChunks { get => inactiveChunks; }

    private void Awake()
    {
      cameraTransform = Camera.main.transform;
      entityTransform = GameObject.FindGameObjectWithTag("Player").transform;

      starGenerator = GetComponent<StarGenerator>();
    }

    private void Start()
    {
      maxOriginDistance += chunkDiameter;

      entityAbsoluteChunkPosition = ChunkUtils.GetEntityChunkPosition(entityAbsolutePosition, chunkDiameter);
      entityRelativeChunkPosition = ChunkUtils.GetEntityChunkPosition(entityTransform.position, chunkDiameter);

      GetCurrentChunks(entityAbsoluteChunkPosition, entityRelativeChunkPosition);
    }

    private void Update()
    {
      CalculateEntityAbsolutePosition();
    }

    private void LateUpdate()
    {
      entityAbsoluteChunkPosition = ChunkUtils.GetEntityChunkPosition(entityAbsolutePosition, chunkDiameter);
      entityRelativeChunkPosition = ChunkUtils.GetEntityChunkPosition(entityTransform.position, chunkDiameter);

      if (inactiveChunks.Count > 256)
      {
        StartCoroutine(SaveManager.Instance.SerializeChunks(inactiveChunks));
        inactiveChunks.Clear();
      }

      if (entityAbsoluteChunkPosition != entityLastAbsoluteChunkPosition)
      { 
        DeactivateChunks();
        GetCurrentChunks(entityAbsoluteChunkPosition, entityRelativeChunkPosition);
      }

      // CheckFloatingOrigins();
      entityLastAbsoluteChunkPosition = entityAbsoluteChunkPosition;
      entityLastPosition = entityTransform.position;
    }

    private void DeactivateChunks()
    {
      foreach (var lazyChunk in lazyChunks)
      {
        if (inactiveChunks.ContainsKey(lazyChunk.Key)) continue;
        inactiveChunks.Add(lazyChunk.Key, lazyChunk.Value);

        lazyChunk.Value.DeactivateChunkObject();
      }

      foreach (var activeChunk in activeChunks)
      {
        if (inactiveChunks.ContainsKey(activeChunk.Key)) continue;
        inactiveChunks.Add(activeChunk.Key, activeChunk.Value);
        activeChunk.Value.DeactivateChunkObject();
      }

      lazyChunks.Clear();
      activeChunks.Clear();
    }

    private void GetCurrentChunks(Vector2Int _entityAbsoluteChunkPosition, Vector2Int _entityRelativeChunkPosition)
    {
      for (int x = -5; x <= 5; x++)
      {
        for (int y = -5; y <= 5; y++)
        {
          Vector2Int chunkPosition = new Vector2Int(
            _entityAbsoluteChunkPosition.x + x,
            _entityAbsoluteChunkPosition.y + y
          );

          Vector2Int chunkRelativePosition = new Vector2Int(
            _entityRelativeChunkPosition.x + x,
            _entityRelativeChunkPosition.y + y
          );

          Vector2Int chunkCellPosition = ChunkUtils.GetChunkGroup(chunkPosition);

          if (inactiveChunks.ContainsKey(chunkPosition))
          {
            if (!lazyChunks.ContainsKey(chunkPosition))
            {
              lazyChunks.Add(chunkPosition, inactiveChunks[chunkPosition]);
            }

            inactiveChunks.Remove(chunkPosition);
          }
          //Non-Functioning
          // else if (SaveManager.Instance.DoesCellFileExist(chunkCellPosition))
          // {
          //   var chunkListLoaded = LoadChunkCell(chunkCellPosition, preCheckedForFile: true);

          //   foreach (var chunk in chunkListLoaded.chunksDict)
          //   {
          //     if (inactiveChunks.ContainsKey(chunk.Key)) continue;
          //     inactiveChunks.Add(chunk.Key, chunk.Value);
          //   }

          //   if (inactiveChunks.ContainsKey(chunkPosition))
          //   {
          //     lazyChunks.Add(chunkPosition, inactiveChunks[chunkPosition]);

          //     inactiveChunks.Remove(chunkPosition);
          //   }
          //   else
          //   {
          //     if (lazyChunks.ContainsKey(chunkPosition)) continue;
          //     lazyChunks.Add(chunkPosition, GenerateChunk(chunkPosition, chunkRelativePosition * chunkDiameter));
          //   }
          // }
          else
          {
            if (lazyChunks.ContainsKey(chunkPosition)) continue;
            lazyChunks.Add(chunkPosition, GenerateChunk(chunkPosition, chunkRelativePosition * chunkDiameter));
          }

          if (Math.Abs(x) <= 3 && Math.Abs(y) <= 3)
          {
            Chunk activeChunk = lazyChunks[chunkPosition];

            if (!activeChunks.ContainsKey(activeChunk.ChunkKey))
            {
              activeChunks.Add(activeChunk.ChunkKey, activeChunk);
              activeChunk.SetChunkObject(chunkRelativePosition * chunkDiameter);
            }

            lazyChunks.Remove(chunkPosition);
          }
        }
      }
    }

    private ChunkListSerializable LoadChunkCell(Vector2Int cellKey, bool preCheckedForFile = false)
    {
      return SaveManager.Instance.DeserializeChunkCell(cellKey, preCheckedForFile);
    }

    private Chunk GenerateChunk(Vector2Int chunkKey, Vector2Int chunkRelativePosition)
    {
      var chunk = new Chunk(ChunkIndex, chunkKey, chunkRelativePosition);

      chunk.SetChunkObject(chunkRelativePosition);
      chunk.ChunkObject.SetActive(false);

      if (starGenerator.ShouldSpawnStar(chunkKey))
      {
        var starPosition = starGenerator.GetStarPosition(chunkDiameter);
        chunk.SetStar(starGenerator.GetStar, starPosition);
      }

      return chunk;
    }

    private void CheckFloatingOrigins()
    {
      if (entityTransform.position.magnitude > maxOriginDistance)
      {
        ShiftOrigin(); // Here we shift everything back to origin.
      }
    }

    private void CalculateEntityAbsolutePosition()
    {
      entityAbsolutePosition.x += entityTransform.position.x - entityLastPosition.x;
      entityAbsolutePosition.y += entityTransform.position.y - entityLastPosition.y;
    }

    private void ShiftOrigin()
    {
      CalculateEntityAbsolutePosition();

      Vector2 shiftAmount = entityTransform.position;

      cameraTransform.position = new Vector3(
        cameraTransform.position.x - shiftAmount.x,
        cameraTransform.position.y - shiftAmount.y,
        cameraTransform.position.z
      );

      entityTransform.position = Vector2.zero;
      entityLastPosition = Vector2.zero;
    }

    private long IncrementChunkIndex()
    {
      chunkIndex += 1;
      return chunkIndex;
    }
  }
}
