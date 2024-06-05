using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public enum CelestialBodyType
    {
        Planet,
        Star,
        Moon,
        Asteroid
    }

  [RequireComponent(typeof(OrbitingController))]
  public abstract class CelestialBehaviour : MonoBehaviour, ICelestialBody
  { 
    protected CelestialBodyType _celestialBodyType;
    protected OrbitingController _orbitController;

    protected CelestialBehaviour parentOrbitingBody;
    protected CelestialBehaviour childOrbitingBody;

    protected Rigidbody2D _celestialRigidBody;
    protected Transform _celestialTransform;

    protected Material[] celestialMaterials;
    [SerializeField] public GameObject[] celestialComponents;

    protected int _celestialRadius;
    protected string _celestialName;
    protected float time;

    public CelestialBodyType CelestialBodyType => _celestialBodyType;
    public OrbitingController OrbitController => _orbitController;
    public CelestialBehaviour ParentOrbitingBody => parentOrbitingBody;
    public CelestialBehaviour ChildOrbitingBody => childOrbitingBody;
    public Vector2 WorldPosition => _celestialTransform.position;

    public int GetRadius => _celestialRadius;
    public float Mass => _celestialRigidBody.mass;
    public string CelestialName => _celestialName;
    
    public void SetupCelestialBehaviour(CelestialBodyType type, int radius, string name)
    {
        _celestialBodyType = type;
        _celestialRadius = radius;
        _celestialName = name;
    }

    public void SetOrbitingBody(CelestialBehaviour _parentOrbitingBody)
    {
        parentOrbitingBody = _parentOrbitingBody;
    }

    public void RemoveOrbitingBody()
    {
      parentOrbitingBody = null;
    }

    protected virtual void Awake()
    {
        _orbitController = GetComponent<OrbitingController>();
        _celestialRigidBody = GetComponent<Rigidbody2D>();
        _celestialTransform = transform;

        celestialMaterials = new Material[celestialComponents.Length];
        
        for (int i = 0; i < celestialComponents.Length; i++)
        {
            celestialMaterials[i] = celestialComponents[i].GetComponent<SpriteRenderer>().material;
        }
    }

    protected virtual void Start() { }

    protected virtual void Update()
    {
        if (ParentOrbitingBody is not null && ParentOrbitingBody.CelestialBodyType is CelestialBodyType.Star)
        {
            SetLight(ParentOrbitingBody.WorldPosition);
        }
    }

    protected virtual void UpdateTime(float time)
    {
      for (int i = 0; i < celestialMaterials.Length; i++)
      {
        celestialMaterials[i].SetFloat(ShaderProperties.Key_time, time);
      }
    }

    public void SetLight(Vector2 lightPosition, float lightModifier = 1f)
    {
        Vector2 relativePos = lightPosition - (Vector2)transform.position;
        relativePos = relativePos.normalized * lightModifier;

        Vector2 viewportPos = relativePos * 0.5f + new Vector2(0.5f, 0.5f);

        for (int i = 0; i < celestialMaterials.Length; i++)
        {
            celestialMaterials[i].SetVector(ShaderProperties.Key_Light_origin, viewportPos);
        }
    }


    // public CelestialBodyType CelestialBodyType { get; }
    // public void SetSeed(float seed) {}
    // public void SetRotate(float r) {}
    // public void SetCustomTime(float time) {}
    // public Color[] GetColors() { return new Color[0]; }
    // // void SetColors(Color[] _colors);
    // public void SetInitialColors() {}
    public float MaxOrbitRadius { get; set; }
    public float Temperature { get; private set; }
    public bool IsOrbiting { get; private set; }
  }
}