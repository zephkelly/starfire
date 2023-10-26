using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;

namespace Starfire.WorldGen
{
  public class ChunkManager : MonoBehaviour
  {
    [SerializeField] static int chunkDiameter = 100;
    [SerializeField] static int maxOriginDistance = 4000; // How far we can get from origin before we shift back to origin

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
      entityTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
      entityAbsolutePosition.x += entityTransform.position.x - entityLastPosition.x;
      entityAbsolutePosition.y += entityTransform.position.y - entityLastPosition.y;

      entityChunkPosition = GetEntityChunkPosition(entityAbsolutePosition);

      if (Vector2.Distance(entityTransform.position, Vector2.zero) > maxOriginDistance)
      {
        ShiftOrigin(); // Here we shift everything back to origin.
      }

      entityLastPosition = entityTransform.position;
    }

    private Vector2Int GetEntityChunkPosition(Vector2D position)
    {
      return new Vector2Int(
        Mathf.FloorToInt((float)entityAbsolutePosition.x / chunkDiameter),
        Mathf.FloorToInt((float)entityAbsolutePosition.y / chunkDiameter)
      );
    }

    private void ShiftOrigin()
    {
      entityTransform.position = Vector2.zero;
      entityLastPosition = Vector2.zero;

      Debug.Log("Abs Pos X: " + entityAbsolutePosition.x + ", Abs Pos Y: " + entityAbsolutePosition.y);
      Debug.Log("Chunk position: " + entityChunkPosition.x + ", " + entityChunkPosition.y);
    }
  }
}
