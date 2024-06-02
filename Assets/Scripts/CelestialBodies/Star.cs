using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
public class Star
{
    private StarController starController;
    private GameObject starObject = null;

    private Vector2Int chunkAbsKey;
    private Vector2Int currentWorldKey;
    [SerializeField] private Vector2 starChunkOffset;
    [SerializeField] private string starName;
    [SerializeField] private int starRadius;
    private float starRotation;
    const int maxRadius = 3500;
    const int minRadius = 2000;

    public GameObject GetStarObject { get => starObject; }
    public Vector2 GetStarOffset { get => starChunkOffset; }
    public Vector2 GetStarPosition { get => currentWorldKey * ChunkManager.Instance.ChunkDiameter + starChunkOffset; }

    public Star(Vector2 chunkOffset, Vector2Int key, int radius, string name)
    {
        chunkAbsKey = key;
        starChunkOffset = chunkOffset;
        starRadius = radius;
        starName = name;

        starRotation = Random.Range(0, 360);
    }

    public void SetStarObject(Vector2Int worldKey)
    {
        currentWorldKey = worldKey;

        if (starObject == null)
        {
            starObject = ChunkManager.Instance.StarGenerator.StarPool.Get();
            starObject.transform.SetParent(ChunkManager.Instance.Chunks[chunkAbsKey].ChunkObject.transform);
        }
        
        StarController starController = starObject.GetComponent<StarController>();
        SetStarProperties(starController, starRadius);
        SetStarVisuals(starController, starRadius);
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

    private void SetStarProperties(StarController _controller, int _radius)
    {
        _controller.SetupCelestialBehaviour(CelestialBodyType.Star, starRadius, starName);

        _controller.GetStarRadiusCollider.radius = starRadius;
        _controller.GetStarLight.pointLightOuterRadius = starRadius;
        starObject.transform.position = GetStarPosition;
    }

    private void SetStarVisuals(StarController _controller, int _radius)
    {
        // Parallax Factor
        float normalizedRadius = Mathf.InverseLerp(minRadius, maxRadius, _radius);
        _controller.GetStarParallaxLayer.SetParallaxFactor(Mathf.Lerp(0.87f, 0.95f, normalizedRadius));

        // Visual size of star
        int visualSize = Mathf.RoundToInt(_radius / 5.5f);
        _controller.GetStarVisualTransform.localScale = new Vector3(visualSize, visualSize, 1);

        // Rotate speed
        float rotateSpeed = Mathf.Lerp(0.2f, 0.1f, normalizedRadius);
        _controller.SetRotateSpeedFactor(rotateSpeed);

        // Set pixel count
        _controller.SetPixel(visualSize);

        // Set rotation
        _controller.SetRotate(starRotation);

        // Set random colour
        _controller.SetRandColours();

        //Make star parallax
        CameraController.Instance.starParallaxLayers.Add(_controller.GetStarParallaxLayer);
    }
}
}