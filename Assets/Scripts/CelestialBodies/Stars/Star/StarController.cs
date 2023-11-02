using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Starfire
{
  public class StarController : MonoBehaviour, ICelestialBody
  {
    public CelestialBodyType CelestialBodyType { get; private set; }

    public void SetCelestialBodyType(CelestialBodyType type) => CelestialBodyType = type;
    [SerializeField] private OrbitingController orbitingController;
    public OrbitingController OrbitingController => orbitingController;
    public float MaxOrbitRadius { get; private set; }
    public float Temperature { get; private set; }

    [SerializeField] Light2D starLight;
    [SerializeField] CircleCollider2D starRadiusCollider;
    [SerializeField] SpriteRenderer starSpriteRenderer;

    [SerializeField] private GameObject star;
    [SerializeField] private GameObject starBlobs;
    [SerializeField] private GameObject starFlares;
    private Material m_star;
    private Material m_starBlobs;
    private Material m_starFlares;
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

    private void Start()
    {
      orbitingController = gameObject.GetComponent<OrbitingController>();
      MaxOrbitRadius = 160;
    }

    private void Awake()
    {
      orbitingController = gameObject.GetComponent<OrbitingController>();
      m_star = star.GetComponent<SpriteRenderer>().material;
      m_starBlobs = starBlobs.GetComponent<SpriteRenderer>().material;
      m_starFlares = starFlares.GetComponent<SpriteRenderer>().material;
      SetInitialColors();
    }

    public void SetPixel(float amount)
    {
      m_star.SetFloat(ShaderProperties.Key_Pixels, amount);
      m_starBlobs.SetFloat(ShaderProperties.Key_Pixels, amount * 2);
      m_starFlares.SetFloat(ShaderProperties.Key_Pixels, amount * 2);
    }

    public void SetLight(Vector2 pos)
    {
      return;
    }

    public void SetSeed(float seed)
    {
      var converted_seed = seed % 1000f / 100f;
      m_star.SetFloat(ShaderProperties.Key_Seed, converted_seed);
      m_starBlobs.SetFloat(ShaderProperties.Key_Seed, converted_seed);
      m_starFlares.SetFloat(ShaderProperties.Key_Seed, converted_seed);
      setGragientColor();
    }

    public void SetRotate(float r)
    {
      m_star.SetFloat(ShaderProperties.Key_Rotation, r);
      m_starBlobs.SetFloat(ShaderProperties.Key_Rotation, r);
      m_starFlares.SetFloat(ShaderProperties.Key_Rotation, r);
    }

    public void UpdateTime(float time)
    {
      m_star.SetFloat(ShaderProperties.Key_time, time * 0.1f);
      m_starBlobs.SetFloat(ShaderProperties.Key_time, time);
      m_starFlares.SetFloat(ShaderProperties.Key_time, time);
    }

    public void SetCustomTime(float time)
    {
      m_star.SetFloat(ShaderProperties.Key_time, time);
      m_starBlobs.SetFloat(ShaderProperties.Key_time, time);
      m_starFlares.SetFloat(ShaderProperties.Key_time, time);
    }

    public void SetInitialColors()
    {
      setGragientColor();

      m_starBlobs.SetColor(color_vars1[0], ColorUtil.FromRGB(init_colors1[0]));
    }

    private void setGragientColor()
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

      m_star.SetTexture(gradient_vars, starTexture);
      m_starFlares.SetTexture(gradient_vars, flareTexture);
    }

    public Color[] GetColors()
    {
      var colors = new Color[7];
      for (int i = 0; i < color_vars1.Length; i++)
      {
        colors[i] = m_starBlobs.GetColor(color_vars1[i]);
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
        m_starBlobs.SetColor(color_vars1[i], _colors[i]);
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