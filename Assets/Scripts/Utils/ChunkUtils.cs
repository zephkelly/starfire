using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Utils
{
  public static class ChunkUtils
  {
    private static int chunkCellSize = 100;

    public static string GetChunkGroupIndex(Vector2Int key)
    {
      int cellX = Mathf.FloorToInt(key.x / chunkCellSize);
      int cellY = Mathf.FloorToInt(key.y / chunkCellSize);
      return $"cell{cellX}_{cellY}";
    }
  }
}