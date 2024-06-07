using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Starfire
{
  public class StarGenerator : MonoBehaviour
  {
    private GameObject starPrefab;
    private ObjectPool<GameObject> starPool;

    [SerializeField] private float noiseScale = 0.1f; // Smaller values make smoother noise.
    [SerializeField] private float starSpawnThreshold = 0.7f; // Threshold for spawning a star.
    [Range(0f, 100f)]
    [SerializeField] private float spawnChance = 1f; // Chance of spawning a star.

    public ObjectPool<GameObject> StarPool { get => starPool; }

    public void Awake()
    {
        starPrefab = Resources.Load<GameObject>("Prefabs/Stars/Star");

        starPool = new ObjectPool<GameObject>(() => 
        {
            return Instantiate(starPrefab);
        }, _starObject => 
        {
            _starObject.SetActive(true);
        }, _starObject => 
        {
            _starObject.SetActive(false);
        }, _starObject => 
        {
            Destroy(_starObject);
        }, false, 5, 10);
    }

    public bool ShouldSpawnStar(Vector2Int chunkKey, bool makeStar = false, bool preventMakeStar = false)
    {
        if (preventMakeStar == true) return false;
        if(makeStar == true) return true;

        float perlinValue = Mathf.PerlinNoise(chunkKey.x * noiseScale, chunkKey.y * noiseScale);

        if (perlinValue > starSpawnThreshold)
        {
            if (Random.Range(0f, 100f) > spawnChance) return false;

            var searchDistance = 5;
            for (int x = -searchDistance; x <= searchDistance; x++)
            {
                for (int y = -searchDistance; y <= searchDistance; y++)
                {
                    Vector2Int searchChunkKey = new Vector2Int(
                    chunkKey.x + x,
                    chunkKey.y + y
                    );

                    if (ChunkManager.Instance.Chunks.ContainsKey(searchChunkKey) && ChunkManager.Instance.Chunks[searchChunkKey].HasStar)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        return false;
    }

    public Vector2 GetStarPosition(int chunkDiameter, float divisionFactor = 3f)
    {
        return new Vector2(
            Random.Range(-chunkDiameter / divisionFactor, chunkDiameter / divisionFactor),
            Random.Range(-chunkDiameter / divisionFactor, chunkDiameter / divisionFactor)
        );
    }

    public int GetStarRadius()
    {
        return Random.Range(3000, 4000);
    }

    public float GetStarMass(StarType starType, float radius)
    {
        switch (starType)
        {
            case StarType.NeutronStar:
                return 900000;
            case StarType.WhiteDwarf:
                return 900000;
            case StarType.RedGiant:
                return 900000;
            case StarType.YellowDwarf:
                return 900000;
            case StarType.BlueGiant:
                return 900000;
            default:
                return 900000;
        }
    }
  }
}