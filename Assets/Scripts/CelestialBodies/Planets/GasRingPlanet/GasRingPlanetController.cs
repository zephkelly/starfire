using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfire
{
  public class GasRingPlanetController : CelestialBehaviour, IPlanet
  {
    public PlanetType PlanetType { get; private set; }
    
    [SerializeField] private GradientTextureGenerate _gradientGas1;
    [SerializeField] private GradientTextureGenerate _gradientGas2;
    [SerializeField] private GradientTextureGenerate _gradientRing1;
    [SerializeField] private GradientTextureGenerate _gradientRing2;

    private string gradient_vars = "_ColorScheme";
    private string gradient_dark_vars = "_Dark_ColorScheme";
    private GradientColorKey[] colorKey1 = new GradientColorKey[3];
    private GradientColorKey[] colorKey2 = new GradientColorKey[3];
    
    private GradientAlphaKey[] alphaKey = new GradientAlphaKey[3];
    
    private string[] _colors1 = new[] {"#eec39a", "#d9a066", "#8f563b"};
    private string[] _colors2 = new[] {"#663931", "#45283c", "#222034"};
    private float[] _color_times = new float[] { 0, 0.5f, 1.0f };

    public void SetPlanetType(PlanetType type) => PlanetType = type;

    protected override void Awake()
    {
      base.Awake();
      SetGradientColor();
    }

    protected override void Update()
    {
      time += Time.deltaTime;
      UpdateTime(Time.time);

      if (ParentOrbitingBody is not null && ParentOrbitingBody.CelestialBodyType is CelestialBodyType.Star)
      {
        SetLight(ParentOrbitingBody.WorldPosition);
      }
    }

    public void SetPixel(float amount)
    {
      celestialMaterials[0].SetFloat(ShaderProperties.Key_Pixels, amount);
      celestialMaterials[1].SetFloat(ShaderProperties.Key_Pixels, amount * 3f);
    }

    public void SetSeed(float seed)
    {
      var converted_seed = seed % 1000f / 100f;
      celestialMaterials[0].SetFloat(ShaderProperties.Key_Seed, converted_seed);
      celestialMaterials[1].SetFloat(ShaderProperties.Key_Seed, converted_seed);
    }

    public void SetRotate(float r)
    {
        celestialMaterials[0].SetFloat(ShaderProperties.Key_Rotation, r);
        celestialMaterials[1].SetFloat(ShaderProperties.Key_Rotation, r + 0.7f);
    }

    protected override void UpdateTime(float time)
    {
      celestialMaterials[0].SetFloat(ShaderProperties.Key_time, time * 0.5f);
      celestialMaterials[1].SetFloat(ShaderProperties.Key_time, time  * 0.5f * -3f);
    }

    public void SetInitialColors()
    {
      SetGradientColor();
    }

    private void SetGradientColor()
    {
      for (int i = 0; i < colorKey1.Length; i++)
      {
        colorKey1[i].color = default(Color);
        ColorUtility.TryParseHtmlString(_colors1[i], out colorKey1[i].color);

        colorKey1[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        alphaKey[i].time = _color_times[i];
      }
        
        
      for (int i = 0; i < colorKey2.Length; i++)
      {
        colorKey2[i].color = default(Color);
        ColorUtility.TryParseHtmlString(_colors2[i], out colorKey2[i].color);

        colorKey2[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        colorKey2[i].time = _color_times[i];
      }
      
      var gasTexture = _gradientGas1.SetColors(colorKey1,alphaKey,gradient_vars);
      var gasTextureDark = _gradientGas2.SetColors(colorKey2,alphaKey,gradient_dark_vars);

      var ringTexture = _gradientRing1.SetColors(colorKey1,alphaKey,gradient_vars);
      var ringTextureDark = _gradientRing2.SetColors(colorKey2, alphaKey,gradient_dark_vars);

      celestialMaterials[0].SetTexture(gradient_vars, gasTexture);
      celestialMaterials[0].SetTexture(gradient_dark_vars, gasTextureDark);

      celestialMaterials[1].SetTexture(gradient_vars, ringTexture);
      celestialMaterials[1].SetTexture(gradient_dark_vars, ringTextureDark);
    }

    public void SetColors(Color[] _colors)
    {
      for (int i = 0; i < colorKey1.Length; i++)
      {
        colorKey1[i].color = _colors[i];
        colorKey1[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        alphaKey[i].time = _color_times[i];
      }
      _gradientGas1.SetColors(colorKey1,alphaKey,gradient_vars);
      var size = colorKey1.Length;
      
      for (int i = 0; i < colorKey2.Length; i++)
      {
        colorKey2[i].color = _colors[ i +size ];
        colorKey2[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        alphaKey[i].time = _color_times[i];
      }
      _gradientGas1.SetColors(colorKey2,alphaKey,gradient_dark_vars);
      size += colorKey2.Length;
      
      for (int i = 0; i < colorKey1.Length; i++)
      {
        colorKey1[i].color = _colors[ i +size ];
        colorKey1[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        alphaKey[i].time = _color_times[i];
      }
      _gradientRing1.SetColors(colorKey1,alphaKey,gradient_vars);
      size += colorKey1.Length;
      
      for (int i = 0; i < colorKey2.Length; i++)
      {
        colorKey2[i].color = _colors[ i +size ];
        colorKey2[i].time = _color_times[i];
        alphaKey[i].alpha = 1.0f;
        alphaKey[i].time = _color_times[i];
      }
      _gradientRing2.SetColors(colorKey2,alphaKey,gradient_dark_vars);  
    }
  }
}