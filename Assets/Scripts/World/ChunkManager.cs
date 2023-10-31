using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Starfire.Utils;
using Unity.VisualScripting;
using System.IO;
using Starfire.IO;

namespace Starfire.Generation
{
  public class ChunkManager : MonoBehaviour
  {
    [SerializeField] static int chunkDiameter = 100;
    [SerializeField] static int maxOriginDistance = 4000;

    private Transform cameraTransform;
    private Transform entityTransform;
    private Vector2 entityLastPosition;
    private Vector2D entityAbsolutePosition;

    private Vector2Int entityChunkPosition;
    private Vector2Int entityLastChunkPosition;

    private Dictionary<Vector2Int, Chunk> activeChunks = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<Vector2Int, Chunk> lazyChunks = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<Vector2Int, Chunk> inactiveChunks = new Dictionary<Vector2Int, Chunk>();
    private long chunkIndex = -1;

    private long ChunkIndex { get => chunkIndex++; }

    public int ChunkDiameter { get => chunkDiameter; }
    public int MaxOriginDistance { get => maxOriginDistance; }

    private void Awake()
    {
      cameraTransform = Camera.main.transform;
      entityTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
      maxOriginDistance += chunkDiameter / 2;
    }

    private void Update()
    {
      CalculateEntityAbsolutePosition();
    }

    private void LateUpdate()
    {
      entityChunkPosition = GetEntityChunkPosition(entityAbsolutePosition);

      if (entityChunkPosition != entityLastChunkPosition)
      { 
        DeactivateChunks();
        GetCurrentChunks();
        Debug.Log($"Active chunks: {activeChunks.Count}");
        Debug.Log("Lazy chunks: " + lazyChunks.Count);
        Debug.Log("Inactive chunks: " + inactiveChunks.Count);
      }

      if (inactiveChunks.Count > 1024)
      {
        SaveManager.Instance.SerializeChunks(inactiveChunks);
        inactiveChunks.Clear();
      }

      CheckFloatingOrigins();
    }

    private void CheckFloatingOrigins()
    {
      if (entityTransform.position.magnitude > maxOriginDistance)
      {
        ShiftOrigin(); // Here we shift everything back to origin.
      }

      entityLastChunkPosition = entityChunkPosition;
      entityLastPosition = entityTransform.position;
    }

    private void DeactivateChunks()
    {
      foreach (var lazyChunk in lazyChunks)
      {
        if (inactiveChunks.ContainsKey(lazyChunk.Key)) continue;
        inactiveChunks.Add(lazyChunk.Key, lazyChunk.Value);
      }

      foreach (var activeChunk in activeChunks)
      {
        if (inactiveChunks.ContainsKey(activeChunk.Key)) continue;
        inactiveChunks.Add(activeChunk.Key, activeChunk.Value);
      }

      lazyChunks.Clear();
      activeChunks.Clear();
    }

    private void GetCurrentChunks()
    {
      for (int x = -7; x <= 7; x++)
      {
        for (int y = -7; y <= 7; y++)
        {
          Vector2Int chunkPosition = new Vector2Int(
            entityChunkPosition.x + x,
            entityChunkPosition.y + y
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
          else if (SaveManager.Instance.DoesCellFileExist(chunkCellPosition))
          {
            var chunkListLoaded = LoadChunkCell(chunkCellPosition, preCheckedForFile: true);

            foreach (var chunk in chunkListLoaded.chunksDict)
            {
              if (inactiveChunks.ContainsKey(chunk.Key)) continue;
              inactiveChunks.Add(chunk.Key, chunk.Value);
            }

            if (inactiveChunks.ContainsKey(chunkPosition))
            {
              lazyChunks.Add(chunkPosition, inactiveChunks[chunkPosition]);
              inactiveChunks.Remove(chunkPosition);
            }
            else
            {
              if (!lazyChunks.ContainsKey(chunkPosition))
              {
                lazyChunks.Add(chunkPosition, GenerateChunk(chunkPosition));
              }
            }
          }
          else
          {
            if (!lazyChunks.ContainsKey(chunkPosition))
            {
              lazyChunks.Add(chunkPosition, GenerateChunk(chunkPosition));
            }
          }

          if (Math.Abs(x) <= 3 && Math.Abs(y) <= 3)
          {
            Chunk activeChunk = lazyChunks[chunkPosition];

            if (!activeChunks.ContainsKey(activeChunk.ChunkKey))
            {
              activeChunks.Add(activeChunk.ChunkKey, activeChunk);
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

    private Chunk GenerateChunk(Vector2Int chunkKey)
    {
      return new Chunk(ChunkIndex, chunkKey);
    }

    private void CalculateEntityAbsolutePosition()
    {
      entityAbsolutePosition.x += entityTransform.position.x - entityLastPosition.x;
      entityAbsolutePosition.y += entityTransform.position.y - entityLastPosition.y;
    }

    private Vector2Int GetEntityChunkPosition(Vector2D position)
    {
      return new Vector2Int(
        Mathf.FloorToInt((float)position.x / chunkDiameter),
        Mathf.FloorToInt((float)position.y / chunkDiameter)
      );
    }

    private void ShiftOrigin()
    {
      cameraTransform.position = new Vector3(
        cameraTransform.position.x - entityTransform.position.x,
        cameraTransform.position.y - entityTransform.position.y,
        cameraTransform.position.z
      );

      entityTransform.position = Vector2.zero;
      entityLastPosition = Vector2.zero;
    }
  }
}
