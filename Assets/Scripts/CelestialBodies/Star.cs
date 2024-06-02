using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
public class Star
{
    private StarController starController;
    private GameObject starObject;

    private Vector2Int chunkAbsKey;
    private Vector2Int currentWorldKey;
    [SerializeField] private Vector2 starChunkOffset;
    [SerializeField] private int starRadius;
    [SerializeField] private string starName;

    public GameObject GetStarObject { get => starObject; }
    public Vector2 GetStarOffset { get => starChunkOffset; }
    public Vector2 GetStarPosition { get => currentWorldKey * ChunkManager.Instance.ChunkDiameter + starChunkOffset; }

    public Star(Vector2 chunkOffset, Vector2Int key, int radius, string name)
    {
        chunkAbsKey = key;
        starChunkOffset = chunkOffset;
        starRadius = radius;
        starName = name;
    }

    public void SetStarObject(Vector2Int worldKey)
    {
        currentWorldKey = worldKey;

        if (starObject == null)
        {
            starObject = ChunkManager.Instance.StarGenerator.StarPool.Get();
            starObject.transform.SetParent(ChunkManager.Instance.Chunks[chunkAbsKey].ChunkObject.transform);
        }
        
        CelestialBehaviour starBehavior = starObject.GetComponent<CelestialBehaviour>();
        starBehavior.SetupCelestialBehaviour(CelestialBodyType.Star, starRadius, starName);
        starObject.transform.position = GetStarPosition;

        CameraController.Instance.starParallaxLayers.Add(starObject.GetComponent<StarParallaxLayer>());
    }

    public void RemoveStarObject()
    {
        if (starObject != null)
        {
            CameraController.Instance.starParallaxLayers.Remove(starObject.GetComponent<StarParallaxLayer>());

            ChunkManager.Instance.StarGenerator.StarPool.Release(starObject);
            starObject = null;
        }
    }
}
}