using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Utils
{
  public static class ChunkUtils
  {
    private static int chunkCellSize = 16;

    public static Vector2Int GetChunkGroup(Vector2Int key)
    {
      int cellX = key.x >= 0 ? Mathf.FloorToInt(key.x / chunkCellSize) : Mathf.CeilToInt(key.x / chunkCellSize) - 1;
      int cellY = key.y >= 0 ? Mathf.FloorToInt(key.y / chunkCellSize) : Mathf.CeilToInt(key.y / chunkCellSize) - 1;
      return new Vector2Int(cellX, cellY);
    } 
  }
}