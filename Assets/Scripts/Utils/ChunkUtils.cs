using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
  public static class ChunkUtils
  {
    private static int chunkCellSize = 16;

    public static Vector2Int GetChunkCell(Vector2Int key)
    {
      int cellX = key.x >= 0 ? Mathf.FloorToInt(key.x / chunkCellSize) : Mathf.CeilToInt(key.x / chunkCellSize) - 1;
      int cellY = key.y >= 0 ? Mathf.FloorToInt(key.y / chunkCellSize) : Mathf.CeilToInt(key.y / chunkCellSize) - 1;
      return new Vector2Int(cellX, cellY);
    } 

    public static Vector2Int GetEntityChunkPosition(Vector2D position, int chunkDiameter)
    {
      return new Vector2Int(
        Mathf.RoundToInt((float)position.x / chunkDiameter),
        Mathf.RoundToInt((float)position.y / chunkDiameter)
      );
    }

    public static Vector2Int GetEntityChunkPosition(Vector3 position, int chunkDiameter)
    {
      return new Vector2Int(
        Mathf.RoundToInt(position.x / chunkDiameter),
        Mathf.RoundToInt(position.y / chunkDiameter)
      );
    }
  }
}