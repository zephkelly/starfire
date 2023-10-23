using System.Collections.Generic;
using UnityEngine;

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
}

public interface IPlanet 
{
  PlanetType PlanetType { get; }
  void SetPlanetType(PlanetType type);
}