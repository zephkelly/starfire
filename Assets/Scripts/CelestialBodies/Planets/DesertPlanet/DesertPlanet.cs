using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfire
{
  public class DesertPlanet : CelestialBehaviour
  {
    public PlanetType PlanetType { get; private set; }

    [SerializeField] private GameObject Land;
    [SerializeField] private GradientTextureGenerate _gradientLand;

    private GradientColorKey[] colorKey = new GradientColorKey[5];
    private GradientAlphaKey[] alphaKey = new GradientAlphaKey[5];

    private string[] _colors1 = new[] {"#ff8933", "#e64539", "#ad2f45", "#52333f", "#3d2936"};
    private float[] _color_times = new float[] { 0, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };

    public void SetPlanetType(PlanetType type) => PlanetType = type;

    protected override void Awake()
    {
      base.Awake();

      _celestialBodyType = CelestialBodyType.Planet;
    }

    protected override void Start()
    {
        base.Start();

        SetInitialColors();
    }

    protected override void Update()
    {
        UpdateTime(Time.time);

        if (ParentOrbitingBody is not null && ParentOrbitingBody.CelestialBodyType is CelestialBodyType.Star)
        {
            SetLight(ParentOrbitingBody.WorldPosition, 0.8f);
        }
    }

    public void SetSeed(float seed)
    {
        var converted_seed = seed % 1000f / 100f;
        _celestialMaterials[0].SetFloat(ShaderProperties.Key_Seed, converted_seed);
    }

    public void SetRotate(float r)
    {
        _celestialMaterials[0].SetFloat(ShaderProperties.Key_Rotation, r);
    }

    public void SetCustomTime(float time)
    {
        var dt = 10f + time * 60f;
        _celestialMaterials[0].SetFloat(ShaderProperties.Key_time, dt);
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