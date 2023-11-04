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
    }
  }
}
