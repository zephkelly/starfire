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
    const int chunkDiameter = 300;
    private int maxOriginDistance = 4000;

    private Transform cameraTransform;
    private Transform entityTransform;
    private Vector2 entityLastPosition;
    private Vector2D entityAbsolutePosition;

    private Vector2Int entityAbsoluteChunkPosition;
    private Vector2Int entityLastAbsoluteChunkPosition;

    private Vector2Int entityRelativeChunkPosition;
    private Vector2Int entityLastRelativeChunkPosition;

    [SerializeField] private float noiseScale = 0.02f; // Smaller values make smoother noise.
    [SerializeField] private float starSpawnThreshold = 0.7f; // Threshold for spawning a star.

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
      entityAbsoluteChunkPosition = GetEntityChunkPosition(entityAbsolutePosition);
      entityRelativeChunkPosition = GetEntityChunkPosition(entityTransform.position);


      if (inactiveChunks.Count > 512)
      {
        StartCoroutine(SaveManager.Instance.SerializeChunks(inactiveChunks));
        inactiveChunks.Clear();
      }

      if (entityAbsoluteChunkPosition != entityLastAbsoluteChunkPosition)
      { 
        DeactivateChunks();
        GetCurrentChunks();
        // Debug.Log($"Active chunks: {activeChunks.Count}");
        // Debug.Log("Lazy chunks: " + lazyChunks.Count);
        // Debug.Log("Inactive chunks: " + inactiveChunks.Count);
      }

      CheckFloatingOrigins();
    }

    private void CheckFloatingOrigins()
    {
      if (entityTransform.position.magnitude > maxOriginDistance)
      {
        ShiftOrigin(); // Here we shift everything back to origin.
      }

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

    private void GetCurrentChunks()
    {
      for (int x = -3; x <= 3; x++)
      {
        for (int y = -3; y <= 3; y++)
        {
          Vector2Int chunkPosition = new Vector2Int(
            entityAbsoluteChunkPosition.x + x,
            entityAbsoluteChunkPosition.y + y
          );

          Vector2Int chunkRelativePosition = new Vector2Int(
            entityRelativeChunkPosition.x + x,
            entityRelativeChunkPosition.y + y
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
                lazyChunks.Add(chunkPosition, GenerateChunk(chunkPosition, chunkRelativePosition));
              }
            }
          }
          else
          {
            if (!lazyChunks.ContainsKey(chunkPosition))
            {
              lazyChunks.Add(chunkPosition, GenerateChunk(chunkPosition, chunkRelativePosition * chunkDiameter));
            }
          }

          if (Math.Abs(x) <= 3 && Math.Abs(y) <= 3)
          {
            Chunk activeChunk = lazyChunks[chunkPosition];

            if (!activeChunks.ContainsKey(activeChunk.ChunkKey))
            {
              activeChunks.Add(activeChunk.ChunkKey, activeChunk);
              PlaceActiveChunk(activeChunk, chunkRelativePosition * chunkDiameter);
            }

            lazyChunks.Remove(chunkPosition);
          }
        }
      }
    }

    private void PlaceActiveChunk(Chunk _chunk, Vector2 _worldPosition)
    {
      _chunk.SetChunkObject(_worldPosition);
    }

    private ChunkListSerializable LoadChunkCell(Vector2Int cellKey, bool preCheckedForFile = false)
    {
      return SaveManager.Instance.DeserializeChunkCell(cellKey, preCheckedForFile);

    }

    private Chunk GenerateChunk(Vector2Int chunkKey, Vector2Int chunkRelativePosition)
    {
      var chunk = new Chunk(ChunkIndex, chunkKey, chunkRelativePosition);

      if (ShouldSetStar(chunkKey))
      {
        Debug.Log("Making Star");

        var newStar = Instantiate(Resources.Load("Prefabs/Stars/Star") as GameObject, Vector3.zero, Quaternion.identity);
        newStar.SetActive(false);

        chunk.SetStar(newStar);
      }

      return chunk;
    }

    private bool ShouldSetStar(Vector2Int chunkKey)
    {
      float perlinValue = Mathf.PerlinNoise(chunkKey.x * noiseScale, chunkKey.y * noiseScale);

      if (perlinValue > starSpawnThreshold)
      {
        // if (UnityEngine.Random.Range(0, 100) > 80) return false; // 1% chance to spawn a star

        //perform a search through the lazy and active chunks in the 3x3 area around the chunk
        for (int x = -5; x <= 5; x++)
        {
          for (int y = -5; y <= 5; y++)
          {
            Vector2Int searchChunkKey = new Vector2Int(
              chunkKey.x + x,
              chunkKey.y + y
            );

            if (inactiveChunks.ContainsKey(searchChunkKey) && inactiveChunks[searchChunkKey].HasStar) 
            {
              Debug.Log("Inactive chunk has star");
              return false;
            }

            if (lazyChunks.ContainsKey(searchChunkKey) && lazyChunks[searchChunkKey].HasStar) 
            {
              Debug.Log("Lazy chunk has star");
              return false;
            }

            if (activeChunks.ContainsKey(searchChunkKey) && activeChunks[searchChunkKey].HasStar) 
            {
              Debug.Log("Active chunk has star");
              return false;
            }
          }
        }

        return true;
      }

      return false;
    }

    private void CalculateEntityAbsolutePosition()
    {
      entityAbsolutePosition.x += entityTransform.position.x - entityLastPosition.x;
      entityAbsolutePosition.y += entityTransform.position.y - entityLastPosition.y;
    }

    private Vector2Int GetEntityChunkPosition(Vector2D position)
    {
      //Create a new FloorToInt method for doubles to remove casting to float
      return new Vector2Int(
        Mathf.FloorToInt((float)position.x / chunkDiameter),
        Mathf.FloorToInt((float)position.y / chunkDiameter)
      );
    }

    private Vector2Int GetEntityChunkPosition(Vector3 position)
    {
      return new Vector2Int(
        Mathf.FloorToInt(position.x / chunkDiameter),
        Mathf.FloorToInt(position.y / chunkDiameter)
      );
    }

    private void ShiftOrigin()
    {
      Vector2 shiftAmount = entityTransform.position;

      cameraTransform.position = new Vector3(
        cameraTransform.position.x - shiftAmount.x,
        cameraTransform.position.y - shiftAmount.y,
        cameraTransform.position.z
      );

      entityAbsolutePosition = new Vector2D(entityAbsolutePosition.x - shiftAmount.x, entityAbsolutePosition.y - shiftAmount.y);

      entityTransform.position = Vector2.zero;
      entityLastPosition = Vector2.zero;
    }
  }
}
