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
    //public static LoadManager LoadManager { get; private set; }

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
      SaveManager.CheckDirectoriesExist("zephyverse");

      List<Chunk> testInactiveChunks = new List<Chunk>();

      for (int i = 0; i < 300; i++)
      {
        testInactiveChunks.Add(new Chunk(i, new Vector2Int(i, i)));
      }

      SaveManager.SaveChunks(testInactiveChunks);

      List<Chunk> testChunksToLoad = new List<Chunk>();
      
      for (int i = 300; i < 600; i++)
      {
        testChunksToLoad.Add(new Chunk(i, new Vector2Int(i, i)));
      }

      SaveManager.SaveChunks(testChunksToLoad);
    }
  }
}
