using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = System.Random;

public class PlanetControl : MonoBehaviour {
    
    [SerializeField] private GameObject[] planets;
    
    private float time = 0f;
    private float pixels = 100;
    private int seed = 0;
    private float rotation = 6.28f;
    private bool override_time = false;

    private void Start()
    {
      OnChangeSeedRandom();
    }

    public void OnSliderPixelChanged(int pixelCount)
    {
      pixels = pixelCount;

      for (int i = 0; i < planets.Length; i++)
      {
        planets[i].GetComponent<IPlanet>().SetPixel(pixels);
      }
    }

    public void OnSliderRotationChanged()
    {
      for (int i = 0; i < planets.Length; i++)
      {
        planets[i].GetComponent<IPlanet>().SetRotate(rotation);
      }
    }

    public void OnLightPositionChanged(Vector2 pos)
    {
      for (int i = 0; i < planets.Length; i++)
      {
        planets[i].GetComponent<IPlanet>().SetLight(pos);
      }
    }

    private void UpdateTime(float time)
    {
      for (int i = 0; i < planets.Length; i++)
      {
        planets[i].GetComponent<IPlanet>().UpdateTime(time);
      }
    }

    public void OnChangeSeed(int newSeed)
    {
      seed = newSeed;

      for (int i = 0; i < planets.Length; i++)
      {
        planets[i].GetComponent<IPlanet>().SetSeed(seed);
      }
    }

    public void OnChangeSeedRandom()
    {
      seedRandom();

      for (int i = 0; i < planets.Length; i++)
      {
        planets[i].GetComponent<IPlanet>().SetSeed(seed);
      }
    }

    private void seedRandom()
    {
      UnityEngine.Random.InitState(System.DateTime.Now.Millisecond );
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