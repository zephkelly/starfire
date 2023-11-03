using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Starfire
{
  [RequireComponent(typeof(OrbitingController))]
  public class StarController : CelestialBehaviour
  {
    [SerializeField] Light2D starLight;
    [SerializeField] CircleCollider2D starRadiusCollider;
    [SerializeField] SpriteRenderer starSpriteRenderer;

    // Order of Celestial Components:
    // private GameObject star;
    // private GameObject starFlares;
    // private GameObject starBlobs;

    [SerializeField] private GradientTextureGenerate _gradientStar;
    [SerializeField] private GradientTextureGenerate _gradientStarFlare;

    private string gradient_vars = "_GradientTex";

    private GradientColorKey[] colorKey1 = new GradientColorKey[4];
    private GradientColorKey[] colorKey2 = new GradientColorKey[2];
    private GradientAlphaKey[] alphaKey1 = new GradientAlphaKey[4];
    private GradientAlphaKey[] alphaKey2 = new GradientAlphaKey[2];

    private string[] color_vars1 = new string[] { "_Color1" };
    private string[] init_colors1 = new string[] { "#acfaec" };
    private string[] _colors1 = new[] { "#9cffed", "#77d6c1", "#1c92a7", "#033e5e" };
    private string[] _colors2 = new[] { "#9cffed", "#c3e0f7" };
    private float[] _color_times1 = new float[4] { 0f, 0.33f, 0.66f, 1.0f };
    private float[] _color_times2 = new float[2] { 0f, 1.0f };

    protected  override void Awake()
    {
      base.Awake();

      _celestialBodyType = CelestialBodyType.Star;
      MaxOrbitRadius = 160;

      SetInitialColors();
    }

    protected override void Update()
    {
      base.Update();
    }

    protected override void UpdateTime(float time)
    {
      celestialMaterials[0].SetFloat(ShaderProperties.Key_time, time * 0.1f);
      celestialMaterials[1].SetFloat(ShaderProperties.Key_time, time);
      celestialMaterials[2].SetFloat(ShaderProperties.Key_time, time);
    }

    public void SetPixel(float amount)
    {
      celestialMaterials[0].SetFloat(ShaderProperties.Key_Pixels, amount);
      celestialMaterials[1].SetFloat(ShaderProperties.Key_Pixels, amount * 2);
      celestialMaterials[2].SetFloat(ShaderProperties.Key_Pixels, amount * 2);
    }

    public void SetSeed(float seed)
    {
      var converted_seed = seed % 1000f / 100f;
      
      for (int i = 0; i < celestialMaterials.Length; i++)
      {
        celestialMaterials[0].SetFloat(ShaderProperties.Key_Seed, converted_seed);
      }

      SetGradientColor();
    }

    public void SetRotate(float r)
    {
      for (int i = 0; i < celestialMaterials.Length; i++)
      {
        celestialMaterials[i].SetFloat(ShaderProperties.Key_Rotation, r);
      }
    }

    public void SetInitialColors()
    {
      SetGradientColor();

      celestialMaterials[2].SetColor(color_vars1[0], ColorUtil.FromRGB(init_colors1[0]));
    }

    private void SetGradientColor()
    {
      for (int i = 0; i < colorKey1.Length; i++)
      {
        colorKey1[i].color = default(Color);
        ColorUtility.TryParseHtmlString(_colors1[i], out colorKey1[i].color);

        colorKey1[i].time = _color_times1[i];
        alphaKey1[i].alpha = 1.0f;
      }

      for (int i = 0; i < colorKey2.Length; i++)
      {
        colorKey2[i].color = default(Color);
        ColorUtility.TryParseHtmlString(_colors2[i], out colorKey2[i].color);

        colorKey2[i].time = _color_times2[i];
        alphaKey2[i].alpha = 1.0f;
      }

      var starTexture = _gradientStar.SetColors(colorKey1, alphaKey1, gradient_vars);
      var flareTexture = _gradientStarFlare.SetColors(colorKey2, alphaKey2, gradient_vars);

      celestialMaterials[0].SetTexture(gradient_vars, starTexture);
      celestialMaterials[1].SetTexture(gradient_vars, flareTexture);
    }

    public Color[] GetColors()
    {
      var colors = new Color[7];
      for (int i = 0; i < color_vars1.Length; i++)
      {
        colors[i] = celestialMaterials[0].GetColor(color_vars1[i]);
      }
      var size = color_vars1.Length;

      var gradColors = _gradientStar.GetColorKeys();
      for (int i = 0; i < gradColors.Length; i++)
      {
        colors[i + size] = gradColors[i].color;
      }
      size += gradColors.Length;

      var gradColors2 = _gradientStarFlare.GetColorKeys();
      for (int i = 0; i < gradColors2.Length; i++)
      {
        colors[i + size] = gradColors2[i].color;
      }

      return colors;
    }

    public void SetColors(Color[] _colors)
    {
      for (int i = 0; i < color_vars1.Length; i++)
      {
        celestialMaterials[2].SetColor(color_vars1[i], _colors[i]);
      }

      var size = color_vars1.Length;
      for (int i = 0; i < colorKey1.Length; i++)
      {
        colorKey1[i].color = _colors[i + size];
        colorKey1[i].time = _color_times1[i];
        alphaKey1[i].alpha = 1.0f;
        alphaKey1[i].time = _color_times1[i];
      }

      _gradientStar.SetColors(colorKey1, alphaKey1, gradient_vars);
      size += colorKey1.Length;

      for (int i = 0; i < colorKey2.Length; i++)
      {
        colorKey2[i].color = _colors[i + size];
        colorKey2[i].time = _color_times2[i];
        alphaKey2[i].alpha = 1.0f;
        alphaKey2[i].time = _color_times2[i];
      }

      _gradientStarFlare.SetColors(colorKey2, alphaKey2, gradient_vars);
    }
  }
}