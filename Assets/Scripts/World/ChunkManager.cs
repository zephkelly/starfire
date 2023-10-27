using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;

namespace Starfire.Generation
{
  public class ChunkManager : MonoBehaviour
  {
    [SerializeField] static int chunkDiameter = 100;
    [SerializeField] static int maxOriginDistance = 100;

    private Transform cameraTransform;
    private Transform entityTransform;
    private Vector2 entityLastPosition;
    private Vector2D entityAbsolutePosition;

    private Vector2Int entityChunkPosition;
    private Vector2Int entityLastChunkPosition;

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

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

    private void LateUpdate()
    {
      entityChunkPosition = GetEntityChunkPosition(entityAbsolutePosition);

      if (entityTransform.position.magnitude > maxOriginDistance)
      {
        ShiftOrigin(); // Here we shift everything back to origin.
      }

      entityLastPosition = entityTransform.position;
    }

    // private void UpdateChunks()
    // {
    //   if (entityChunkPosition != entityLastChunkPosition)
    //   {
    //     for (int x = -1; x <= 1; x++)
    //     {
    //       for (int y = -1; y <= 1; y++)
    //       {
    //         Vector2Int chunkPosition = new Vector2Int(
    //           entityChunkPosition.x + x,
    //           entityChunkPosition.y + y
    //         );

    //         if (!chunks.ContainsKey(chunkPosition))
    //         {
    //           chunks.Add(chunkPosition, new Chunk(chunkPosition, chunkDiameter));
    //         }
    //       }
    //     }
    //   }
    // }

    private Vector2Int GetEntityChunkPosition(Vector2D position)
    {
      entityAbsolutePosition.x += entityTransform.position.x - entityLastPosition.x;
      entityAbsolutePosition.y += entityTransform.position.y - entityLastPosition.y;

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

      Debug.Log("Chunk position: " + entityChunkPosition.x + ", " + entityChunkPosition.y);
    }
  }
}
