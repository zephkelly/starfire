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

  public enum PlanetType
  {
    Rivers,
    Land,
    Gas,
    GasLayers,
    Ice,
    Lava,
    Desert,
    Moon
  }

//Make this an abstract class at some point
  public interface ICelestialBody
  {
    CelestialBodyType CelestialBodyType { get; }
    void SetCelestialBodyType(CelestialBodyType type);
    OrbitingController OrbitController { get; }
    float MaxOrbitRadius { get; }
    float Temperature { get; }
    CelestialBehaviour ParentOrbitingBody { get; }
    CelestialBehaviour ChildOrbitingBody { get; }
    bool IsOrbiting { get; }
    void SetOrbitingBody(CelestialBehaviour orbitingBody);
    void RemoveOrbitingBody();
    Vector2 WorldPosition { get; }
    float Mass { get; }
  }

  public interface IPlanet 
  {
    PlanetType PlanetType { get; }
    void SetPlanetType(PlanetType type);
  }
  
  [RequireComponent(typeof(OrbitingController))]
  public abstract class CelestialBehaviour : MonoBehaviour, ICelestialBody
  { 
    protected CelestialBodyType _celestialBodyType;
    public CelestialBodyType CelestialBodyType => _celestialBodyType;

    protected OrbitingController _orbitController;
    public OrbitingController OrbitController => _orbitController;

    protected Rigidbody2D _celestialRigidBody;
    protected Transform _celestialTransform;
    public Vector2 WorldPosition => _celestialTransform.position;

    protected CelestialBehaviour parentOrbitingBody;
    protected CelestialBehaviour childOrbitingBody;
    public CelestialBehaviour ParentOrbitingBody => parentOrbitingBody;
    public CelestialBehaviour ChildOrbitingBody => childOrbitingBody;

    [SerializeField] public GameObject[] celestialComponents;
    protected Material[] celestialMaterials;

    protected string _celestialName;
    public string CelestialName => _celestialName;

    public float Mass => _celestialRigidBody.mass;

    public void SetCelestialBodyType(CelestialBodyType type) 
    {
      _celestialBodyType = type;
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

    protected float time = 0f;
    protected virtual void Update()
    {
      time += Time.deltaTime;
      UpdateTime(Time.time);

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
    public float Temperature { get; set; }
    public bool IsOrbiting { get; }
  }
}