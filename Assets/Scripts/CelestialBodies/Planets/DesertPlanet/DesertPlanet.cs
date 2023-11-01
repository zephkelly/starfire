using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfire
{
  public class DesertPlanet : MonoBehaviour, ICelestialBody, IPlanet
  {
    public OrbitingController OrbitingController { get; private set; }
    public CelestialBodyType CelestialBodyType { get; private set; }
    public PlanetType PlanetType { get; private set; }

    [SerializeField] private GameObject Land;
    [SerializeField] private GradientTextureGenerate _gradientLand;

    private Material m_Land;
    // private string gradient_vars = "_GradientTex";

    private GradientColorKey[] colorKey = new GradientColorKey[5];
    private GradientAlphaKey[] alphaKey = new GradientAlphaKey[5];

    private string[] _colors1 = new[] {"#ff8933", "#e64539", "#ad2f45", "#52333f", "#3d2936"};
    private float[] _color_times = new float[] { 0, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };

    public void SetCelestialBodyType(CelestialBodyType type) => CelestialBodyType = type;
    public void SetPlanetType(PlanetType type) => PlanetType = type;

    private void Start()
    {
        m_Land = Land.GetComponent<SpriteRenderer>().material;
        SetInitialColors();
    }

    public void SetPixel(float amount)
    {
        m_Land.SetFloat(ShaderProperties.Key_Pixels, amount);
    }

    public void SetLight(Vector2 pos)
    {
        m_Land.SetVector(ShaderProperties.Key_Light_origin, pos);
    }

    public void SetSeed(float seed)
    {
        var converted_seed = seed % 1000f / 100f;
        m_Land.SetFloat(ShaderProperties.Key_Seed, converted_seed);
    }

    public void SetRotate(float r)
    {
        m_Land.SetFloat(ShaderProperties.Key_Rotation, r);
    }

    public void UpdateTime(float time)
    {
        m_Land.SetFloat(ShaderProperties.Key_time, time  );
    }

    public void SetCustomTime(float time)
    {
        var dt = 10f + time * 60f;
        m_Land.SetFloat(ShaderProperties.Key_time, dt);
    }

    public void SetInitialColors()
    {
      SetGradientColor();
    }

    private void SetGradientColor()
    {
      for (int i = 0; i < colorKey.Length; i++)
      {
        colorKey[i].color = default(Color);
        ColorUtility.TryParseHtmlString(_colors1[i], out colorKey[i].color);

        colorKey[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        alphaKey[i].time = _color_times[i];
      }
      _gradientLand.SetColors(colorKey,alphaKey);
    }

    public Color[] GetColors()
    {
      var colors = new Color[5];
      var gradColors = _gradientLand.GetColorKeys();
      for (int i = 0; i < gradColors.Length; i++)
      {
        colors[i] = gradColors[i].color;
      }
      return colors;
    }

    public void SetColors(Color[] _colors)
    {
      for (int i = 0; i < colorKey.Length; i++)
      {
        colorKey[i].color = _colors[i];
        colorKey[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        alphaKey[i].time = _color_times[i];
      }
      _gradientLand.SetColors(colorKey,alphaKey);
    }
  }
}