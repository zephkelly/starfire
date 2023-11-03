using UnityEngine;

namespace Starfire
{
  public class PlanetManager : MonoBehaviour
  {
      [SerializeField] private GameObject[] celestialObjects;
      private ICelestialBody[] celestialBodies;
          
      private float time = 0f;
      private float pixels = 100;
      private int seed = 0;
      private float rotation = 6.28f;
      private bool override_time = false;
  }
}