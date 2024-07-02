using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
public enum StarType
{
    RedDwarf,
    YellowDwarf,
    BlueGiant,
    RedGiant,
    WhiteDwarf,
    NeutronStar,
}

public class Star
{
    public Chunk ParentChunk { get; private set; }
    public CelestialBehaviour CelestialBehaviour { get; private set; }
    public StarType StarType { get; private set; }
    public Vector2 StarPosition { get; private set; }
    public float SurfaceRadius { get; private set; }
    public float InfluenceRadius { get; private set; }
    public float Mass { get; private set; }

    private GameObject starObject = null;


    [SerializeField] private string starName;

    public GameObject GetStarObject { get => starObject; }
    public Vector2 GetStarPosition { get => ParentChunk.CurrentWorldKey * ChunkManager.Instance.ChunkDiameter + StarPosition; }

    public Star(Chunk _parentChunk, Vector2 _starPosition, StarType _type)
    {
        ParentChunk = _parentChunk;
        StarPosition = _starPosition;
        StarType = _type;

        starName = ChunkManager.Instance.NameGenerator.GetStarName();
        SurfaceRadius = ChunkManager.Instance.StarGenerator.GetStarSurfaceRadius();
        InfluenceRadius = ChunkManager.Instance.StarGenerator.GetStarInfluenceRadius();
        Mass = ChunkManager.Instance.StarGenerator.GetStarMass(StarType, InfluenceRadius);
    }

    public CelestialBehaviour SetStarObject()
    {
        if (starObject == null)
        {
            starObject = ChunkManager.Instance.StarGenerator.StarPool.Get();
            starObject.transform.SetParent(ParentChunk.ChunkObject.transform);
        }
        
        StarController celestialBehaviour = starObject.GetComponent<StarController>();
        SetStarProperties(celestialBehaviour);
        // SetStarVisuals(celestialBehaviour);

        return celestialBehaviour;
    }

    public void RemoveStarObject()
    {
        if (starObject != null)
        {
            if (CelestialBehaviour != null && CameraController.Instance.starParallaxLayers.Contains(CelestialBehaviour.GetParallaxLayer))
            {
                CameraController.Instance.starParallaxLayers.Remove(CelestialBehaviour.GetParallaxLayer);
            }

            ChunkManager.Instance.StarGenerator.StarPool.Release(starObject);
            starObject = null;
        }
    }

    private void SetStarProperties(StarController _controller)
    {
        _controller.SetupCelestialBehaviour(CelestialBodyType.Star, InfluenceRadius, Mass, starName);

        _controller.GetStarRigidbody.mass = Mass;
        _controller.GetStarRadiusCollider.radius = InfluenceRadius;
        _controller.GetStarLight.pointLightOuterRadius = InfluenceRadius;
        starObject.transform.position = GetStarPosition;
    }

    private void SetStarVisuals(StarController _controller)
    {
        // Parallax Factor

        // Visual size of star
        int visualSize = Mathf.RoundToInt(InfluenceRadius / 4.5f);
        _controller.GetStarVisualTransform.localScale = new Vector3(visualSize, visualSize, 1);

        // // Rotate speed
        // float rotateSpeed = Mathf.Lerp(0.2f, 0.1f, normalizedRadius);
        // _controller.SetRotateSpeedFactor(rotateSpeed);

        // // Set pixel count
        // _controller.NewPixelAmount(visualSize);

        // // Set rotation
        // _controller.SetRotate(starRotation);

        // // Set random colour
        // _controller.SetRandColours();

        // //Make star parallax
        // CameraController.Instance.starParallaxLayers.Add(_controller.GetParallaxLayer);
    }
}
}