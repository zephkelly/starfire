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
      SaveManager.CheckDirectoriesExist("zephyverse");

      Dictionary<long, Chunk> testDict = new Dictionary<long, Chunk>();

      for (int i = 0; i < 300; i++)
      {
        Chunk newChunk = new Chunk((long)i, new Vector2Int(i, i));

        testDict[newChunk.ChunkIndex] = newChunk;
      }

      SaveManager.SerializeDict(testDict, "zephyverse");
      testDict.Clear();
      
      for (int i = -300; i < 600; i++)
      {
        Chunk newChunk = new Chunk((long)i, new Vector2Int(i, i));

        testDict[newChunk.ChunkIndex] = newChunk;
      }

      SaveManager.SerializeDict(testDict, "zephyverse");

      List<Chunk> testList = new List<Chunk>();

      for (int i = 0; i < 300; i++)
      {
        Chunk newChunk = new Chunk((long)i, new Vector2Int(i, i));

        testList.Add(newChunk);
      }

      SaveManager.SerializeList(testList);
      testList.Clear();

      for (int i = -300; i < 600; i++)
      {
        Chunk newChunk = new Chunk((long)i, new Vector2Int(i, i));

        testList.Add(newChunk);
      }

      SaveManager.SerializeList(testList);
    }
  }
}
