using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Starfire
{
  [RequireComponent(typeof(OrbitingController))]
  public class StarController : CelestialBehaviour
  {
    [SerializeField] Light2D starLight;
    [SerializeField] CircleCollider2D starRadiusCollider;
    [SerializeField] SpriteRenderer starSpriteRenderer;
    [SerializeField] Transform starVisualTransform;
  
    public Rigidbody2D GetStarRigidbody { get => _celestialRigidBody; }
    public Transform GetStarVisualTransform { get => starVisualTransform; }
    public CircleCollider2D GetStarRadiusCollider { get => starRadiusCollider; }
    public Light2D GetStarLight { get => starLight; }

    protected override void Awake()
    {
        _celestialParallaxLayer = GetComponent<CelestialParallaxLayer>();
        _orbitController = GetComponent<OrbitingController>();
        _celestialRigidBody = GetComponent<Rigidbody2D>();
        _celestialRigidBody.mass = Mass;
        _celestialTransform = transform;

        _celestialBodyType = CelestialBodyType.Star;
    }
  }
}