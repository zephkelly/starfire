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
    void SetPixel(float amount);
    void SetLight(Vector2 pos);
    void SetSeed(float seed);
    void SetRotate(float r);
    void UpdateTime(float time);
    void SetCustomTime(float time);
    Color[] GetColors();
    // void SetColors(Color[] _colors);
    void SetInitialColors();
    OrbitingController OrbitController { get; }
    float MaxOrbitRadius { get; }
    float Temperature { get; }
    ICelestialBody ParentOrbitingBody { get; }
    ICelestialBody ChildOrbitingBody { get; }
    bool IsOrbiting { get; }
    void SetOrbitingBody(ICelestialBody orbitingBody);
    void RemoveOrbitingBody();
    Vector2 GetWorldPosition();
  }

  public interface IPlanet 
  {
    PlanetType PlanetType { get; }
    void SetPlanetType(PlanetType type);
  }
}