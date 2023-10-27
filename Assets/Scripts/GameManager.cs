using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Generation;
using Starfire.IO;

namespace Starfire
{
  public class GameManager : MonoBehaviour
  {
    public static GameManager Instance { get; private set; }
    public static SaveManager SaveManager { get; private set; }

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject);
      }
      else
      {
        Destroy(gameObject);
      }

      if (SaveManager == null)
      {
        SaveManager = new SaveManager();
      }
    }

    private void Start()
    {
      List<Chunk> testInactiveChunks = new List<Chunk>();
      testInactiveChunks.Add(new Chunk(1, new Vector2Int(0, 0)));
      testInactiveChunks.Add(new Chunk(2, new Vector2Int(1, 0)));
      testInactiveChunks.Add(new Chunk(3, new Vector2Int(0, 1)));
      testInactiveChunks.Add(new Chunk(4, new Vector2Int(1, 1)));

      SaveManager.SaveChunks(testInactiveChunks);

      List<Chunk> testChunksToLoad = new List<Chunk>();
      testChunksToLoad.Add(new Chunk(2, new Vector2Int(100, 200)));
      testChunksToLoad.Add(new Chunk(5, new Vector2Int(2, 7)));
      testChunksToLoad.Add(new Chunk(6, new Vector2Int(3, 4)));

      SaveManager.SaveChunks(testChunksToLoad);
    }
  }
}
