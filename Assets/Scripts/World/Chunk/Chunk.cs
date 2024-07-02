using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
public enum ChunkState
{
    Active,
    Lazy,
    Inactive
}

[System.Serializable]
public class Chunk : IChunk
{
    // Chunk info
    [SerializeField] private uint chunkIndex;
    [SerializeField] private Vector2Int chunkKey;
    private Vector2Int currentWorldKey;
    private Vector2Int chunkCellKey;
    private GameObject chunkObject = null;
    private ChunkState chunkState = ChunkState.Inactive;

    // Star info
    private Star star = null;

    // Planets info
    private List<Planet> planets = new List<Planet>();

    public long ChunkIndex { get => chunkIndex; }
    public Vector2Int ChunkKey { get => chunkKey; }
    public Vector2Int CurrentWorldKey { get => currentWorldKey; }
    public Vector2Int ChunkCellKey { get => chunkCellKey; }
    public ChunkState ChunkState { get => chunkState; }
    public GameObject ChunkObject { get => chunkObject; }
    public Star GetStar { get => star; }
    public bool HasStar { get; private set; }
    public List<Planet> GetPlanets { get => planets; }
    public bool HasPlanets { get => planets.Count > 0; }
    public Vector2 GetStarPosition { get => currentWorldKey * ChunkManager.Instance.ChunkDiameter + star.StarPosition; }

    public Chunk(uint _chunkIndex, Vector2Int _chunkKey, bool hasStar)
    {
        chunkIndex = _chunkIndex;
        chunkKey = _chunkKey;
        chunkCellKey = ChunkUtils.GetChunkCell(chunkKey);
        HasStar = hasStar;
    }

    public void AddStarToChunk(Star _chunkStar)
    {
        star = _chunkStar;
    }

    public void AddPlanetsToChunk(List<Planet> _chunkPlanets)
    {
        foreach (Planet planet in _chunkPlanets)
        {
            planets.Add(planet);
        }
    }

    public void SetActiveChunk(Vector2Int _chunkCurrentKey)
    {
        chunkState = ChunkState.Active;
        currentWorldKey = _chunkCurrentKey;

        SetChunkObject(_chunkCurrentKey);
        SetStarObject();
    }

    public void SetLazyChunk(Vector2Int _chunkKey)
    {
        chunkState = ChunkState.Lazy;
        currentWorldKey = _chunkKey;

        RemoveStarObject();
        RemoveChunkObject();
    }

    public void SetInactiveChunk()
    {
        chunkState = ChunkState.Inactive;

        RemoveStarObject();
        RemoveChunkObject();
    }

    private void SetChunkObject(Vector2Int _chunkCurrentKey)
    {
        if (chunkObject == null)
        {
            chunkObject = ChunkManager.Instance.ChunkPool.Get();
            chunkObject.name = $"Chunk{chunkIndex}";
            currentWorldKey = _chunkCurrentKey;
            chunkObject.transform.SetParent(ChunkManager.Instance.transform);
        }

        chunkObject.transform.position = GetChunkPosition(currentWorldKey);

        // instantiate a new box collider on the cunk object
        // BoxCollider2D boxCollider = chunkObject.AddComponent<BoxCollider2D>();
        // boxCollider.size = new Vector2(ChunkManager.Instance.ChunkDiameter, ChunkManager.Instance.ChunkDiameter);
        // boxCollider.isTrigger = true;
    }

    private void RemoveChunkObject()
    {
        if (chunkObject == null) return;
        
        ChunkManager.Instance.ChunkPool.Release(chunkObject);
        chunkObject = null;
    }

    private Vector2 GetChunkPosition(Vector2Int _chunkKey)
    {
        Vector2 newPosition = new Vector2(
            _chunkKey.x * ChunkManager.Instance.ChunkDiameter,
            _chunkKey.y * ChunkManager.Instance.ChunkDiameter
        );

        return newPosition;
    }

    private void SetStarObject()
    {
        if (!HasStar) return;
        if (star.GetStarObject != null) return;

        CelestialBehaviour starController = star.SetStarObject();
        SetPlanetObjects(star.GetStarPosition, starController);
    }

    private void RemoveStarObject()
    {
        if (star == null) return;
        if (star.GetStarObject == null) return;

        RemovePlanetObjects();
        star.RemoveStarObject();
    }

    private void SetPlanetObjects(Vector2 _starPosition, CelestialBehaviour _starController)
    {
        foreach (Planet planet in planets)
        {
            GameObject planetObject = planet.SetPlanetObject(_starPosition);
            ICelestialBody planetController = planetObject.GetComponent<ICelestialBody>();
            planetController.SetOrbitingBody(_starController);
        }
    }

    private void RemovePlanetObjects()
    {
        foreach (Planet planet in planets)
        {
            planet.RemovePlanetObject();
        }
    }
}
}