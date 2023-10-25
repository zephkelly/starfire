using UnityEngine;

public class PlanetManager : MonoBehaviour
{
    [SerializeField] private GameObject[] celestialObjects;
    private ICelestialBody[] celestialBodies;
        
    private float time = 0f;
    private float pixels = 100;
    private int seed = 0;
    private float rotation = 6.28f;
    private bool override_time = false;

    private void Start()
    {
      UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
      // OnChangeSeedRandom();

      celestialBodies = new ICelestialBody[celestialObjects.Length];
      for (int i = 0; i < celestialObjects.Length; i++)
      {
        celestialBodies[i] = celestialObjects[i].GetComponent<ICelestialBody>();
      }
    }

    public void OnSliderPixelChanged(int pixelCount)
    {
      pixels = pixelCount;

      for (int i = 0; i < celestialBodies.Length; i++)
      {
        celestialBodies[i].SetPixel(pixels);
      }
    }

    public void OnSliderRotationChanged()
    {
      for (int i = 0; i < celestialBodies.Length; i++)
      {
        celestialBodies[i].SetRotate(rotation);
      }
    }

    public void OnLightPositionChanged(Vector2 pos)
    {
      for (int i = 0; i < celestialBodies.Length; i++)
      {
        celestialBodies[i].SetLight(pos);
      }
    }

    private void UpdateTime(float time)
    {
      for (int i = 0; i < celestialBodies.Length; i++)
      {
        celestialBodies[i].UpdateTime(time);
      }
    }

    public void OnChangeSeedRandom()
    {
      for (int i = 0; i < celestialBodies.Length; i++)
      {
        SeedRandom();
        celestialBodies[i].SetSeed(seed);
      }
    }

    private void SeedRandom()
    {
      seed = UnityEngine.Random.Range(0, int.MaxValue);
    }

    private void Update()
    {
      if (Input.GetMouseButton(0))
      {
        var pos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        OnLightPositionChanged(pos);
      }

      time += Time.deltaTime;

      if (override_time) return;
      
      UpdateTime(time);
    }
}
