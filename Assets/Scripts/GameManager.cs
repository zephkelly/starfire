using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        SaveManager = new SaveManager("zephyverse");
      }
    }

    private void Start()
    {
      SaveManager.CheckDirectoriesExist();

      // Dictionary<long, Chunk> testDict = new Dictionary<long, Chunk>();
      // for (int i = 0; i < 300; i++)
      // {
      //     Chunk newChunk = new Chunk((long)i, new Vector2Int(i, i));
      //     testDict[newChunk.ChunkIndex] = newChunk;
      // }

      // Stopwatch sw = new Stopwatch();

      // // Timing SerializeDict method
      // sw.Start();
      // SaveManager.SerializeChunks(testDict);
      // sw.Stop();
      // UnityEngine.Debug.Log($"SerializeDict took {sw.Elapsed.TotalMilliseconds} milliseconds");
      // testDict.Clear();

      // for (int i = -600; i < 600; i++)
      // {
      //     Chunk newChunk = new Chunk((long)i, new Vector2Int(i, i));
      //     testDict[newChunk.ChunkIndex] = newChunk;
      // }

      // // Timing SerializeDict method again
      // sw.Restart();
      // // SaveManager.SerializeChunks(testDict);
      // sw.Stop();
      // UnityEngine.Debug.Log($"SerializeDict took {sw.Elapsed.TotalMilliseconds} milliseconds");

      // List<Chunk> testList = new List<Chunk>();
      // for (int i = 0; i < 300; i++)
      // {
      //     Chunk newChunk = new Chunk((long)i, new Vector2Int(i, i));
      //     testList.Add(newChunk);
      // }
    }
  }
}
