using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
public interface IChunk
{
    long ChunkIndex { get; }
    Vector2Int ChunkKey { get; }
    Vector2Int CurrentWorldKey { get; }
    Vector2Int ChunkCellKey { get; }
    ChunkState ChunkState { get; }
    GameObject ChunkObject { get; }

    // bool HasStar { get; }
    // Vector2 StarPosition { get; }
    // GameObject StarObject { get; }

    void SetActiveChunk(Vector2Int chunkKey);
    void SetLazyChunk(Vector2Int chunkKey);
    void SetInactiveChunk();
}

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
    private List<GameObject> planetObjects = new List<GameObject>();

    public long ChunkIndex { get => chunkIndex; }
    public Vector2Int ChunkKey { get => chunkKey; }
    public Vector2Int CurrentWorldKey { get => currentWorldKey; }
    public Vector2Int ChunkCellKey { get => chunkCellKey; }
    public ChunkState ChunkState { get => chunkState; }
    public GameObject ChunkObject { get => chunkObject; }
    public Star GetStar { get => star; }
    public bool HasStar { get => star != null; }
    public Vector2 GetStarPosition { get => currentWorldKey * ChunkManager.Instance.ChunkDiameter + star.GetStarOffset; }

    public Chunk(uint _chunkIndex, Vector2Int _chunkKey, bool makeStar = false, bool preventMakeStar = false)
    {
        chunkIndex = _chunkIndex;
        chunkKey = _chunkKey;
        chunkCellKey = ChunkUtils.GetChunkCell(chunkKey);

        bool shouldSpawnStar = ChunkManager.Instance.StarGenerator.ShouldSpawnStar(chunkKey, makeStar, preventMakeStar);

        if (shouldSpawnStar)
        {
            Vector2 starPosition = ChunkManager.Instance.StarGenerator.GetStarPosition(ChunkManager.Instance.ChunkDiameter);
            star = new Star(starPosition, chunkKey, StarType.NeutronStar);

            planets = ChunkManager.Instance.PlanetGenerator.GetStarPlanets(star);
        }
    }

    public void SetActiveChunk(Vector2Int _chunkKey)
    {
        chunkState = ChunkState.Active;
        currentWorldKey = _chunkKey;

        SetChunkObject(_chunkKey);
        SetStarObject(_chunkKey);
    }

    public void SetLazyChunk(Vector2Int _chunkKey)
    {
        chunkState = ChunkState.Lazy;
        currentWorldKey = _chunkKey;

        RemoveChunkObject();
        RemoveStarObject();
    }

    public void SetInactiveChunk()
    {
        chunkState = ChunkState.Inactive;

        RemoveChunkObject();
        RemoveStarObject();
    }

    private void SetChunkObject(Vector2Int _chunkKey)
    {
        if (chunkObject == null)
        {
            chunkObject = ChunkManager.Instance.ChunkPool.Get();
            chunkObject.name = $"Chunk{chunkIndex}";
            currentWorldKey = _chunkKey;
            chunkObject.transform.SetParent(ChunkManager.Instance.transform);
            chunkObject.transform.position = GetChunkPosition(currentWorldKey);

            // create a new box colllider with is trigger true and size size of the diameter and place it in world position
            // BoxCollider2D boxCollider = chunkObject.AddComponent<BoxCollider2D>();
            // boxCollider.size = new Vector2(ChunkManager.Instance.ChunkDiameter, ChunkManager.Instance.ChunkDiameter);
            // boxCollider.isTrigger = true;
            return;
        }
        else
        {
            chunkObject.transform.position = GetChunkPosition(currentWorldKey);
        }
    }

    private Vector2 GetChunkPosition(Vector2Int _chunkKey)
    {
        Vector2 newPosition = new Vector2(
            _chunkKey.x * ChunkManager.Instance.ChunkDiameter,
            _chunkKey.y * ChunkManager.Instance.ChunkDiameter
        );

        return newPosition;
    }

    private void RemoveChunkObject()
    {
        if (chunkObject == null) return;
        
        ChunkManager.Instance.ChunkPool.Release(chunkObject);
        chunkObject = null;
    }

    private void SetStarObject(Vector2Int _chunkKey)
    {
        if (!HasStar) return;
        if (star.GetStarObject != null) return;

        CelestialBehaviour starController = star.SetStarObject(_chunkKey);

        foreach (Planet planet in planets)
        {
            GameObject planetObject = planet.SetPlanetObject(_chunkKey, star.GetStarPosition);
            ICelestialBody planetController = planetObject.GetComponent<ICelestialBody>();
            planetController.SetOrbitingBody(starController);
        }
    }

    private void RemoveStarObject()
    {
        if (star == null) return;
        if (star.GetStarObject == null) return;

        star.RemoveStarObject();
    }
}
}