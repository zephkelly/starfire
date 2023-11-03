using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Starfire
{
  [RequireComponent(typeof(OrbitingController))]
  public class GasPlanetController : CelestialBehaviour, IPlanet
  {
    private PlanetType planetType;
    public PlanetType PlanetType => planetType;

    private string[] color_vars1 = new string[]{"_Base_color", "_Outline_color", "_Shadow_base_color","_Shadow_outline_color"};
    private string[] init_colors1 = new string[] {"#3b2027", "#3b2027", "#21181b","#21181b"};
    private string[] color_vars2 = new string[]{"_Base_color", "_Outline_color", "_Shadow_base_color","_Shadow_outline_color"};
    private string[] init_colors2 = new string[] {"#f0b541", "#cf752b","#ab5130","#7d3833"};

    public void SetPlanetType(PlanetType type) => planetType = type;

    protected override void Awake()
    {
      base.Awake();

      SetInitialColors();
      
      MaxOrbitRadius = 80f;
      _celestialBodyType = CelestialBodyType.Planet;
    }

    protected override void Update()
    {
      time += Time.deltaTime;
      UpdateTime(Time.time);

      if (ParentOrbitingBody is not null && ParentOrbitingBody.CelestialBodyType is CelestialBodyType.Star)
      {
        SetLight(ParentOrbitingBody.WorldPosition, 0.9f);
      }
    }

    public void SetPixel(float amount)
    {
      celestialMaterials[0].SetFloat(ShaderProperties.Key_Pixels, amount);
      celestialMaterials[1].SetFloat(ShaderProperties.Key_Pixels, amount);
    }

    public void SetInitialColors()
    {
      for (int i = 0; i < color_vars1.Length; i++)
      {
        celestialMaterials[0].SetColor(color_vars1[i], ColorUtil.FromRGB(init_colors1[i]));
      }
      for (int i = 0; i < color_vars2.Length; i++)
      {
        celestialMaterials[1].SetColor(color_vars2[i], ColorUtil.FromRGB(init_colors2[i]));
      }
    }

    public void SetColors(Color[] _colors)
    {
      for (int i = 0; i < color_vars1.Length; i++)
      {
        celestialMaterials[0].SetColor(color_vars1[i], _colors[i]);
      }
      for (int i = 0; i < color_vars2.Length; i++)
      {
        celestialMaterials[1].SetColor(color_vars2[i], _colors[i + color_vars1.Length]);
      }
    }
  }
}