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
    
    [SerializeField] Transform starVisualTransform;
    private float rotateSpeedFactor = 0.1f;

    [SerializeField] private GradientTextureGenerate _gradientStar;
    [SerializeField] private GradientTextureGenerate _gradientStarFlare;

    private string gradient_vars = "_GradientTex";

    private GradientColorKey[] colorKey1 = new GradientColorKey[4];
    private GradientColorKey[] colorKey2 = new GradientColorKey[2];
    private GradientAlphaKey[] alphaKey1 = new GradientAlphaKey[4];
    private GradientAlphaKey[] alphaKey2 = new GradientAlphaKey[2];

    private string[] colorVars1 = new string[] { "_Color1" };
    private string[] initColors1 = new string[] { "#acfaec" };
    private string[] _colors1 = new[] { "#9cffed", "#77d6c1", "#1c92a7", "#033e5e" };
    private string[] _colors2 = new[] { "#9cffed", "#c3e0f7" };
    private float[] _color_times1 = new float[4] { 0f, 0.33f, 0.66f, 1.0f };
    private float[] _color_times2 = new float[2] { 0f, 1.0f };

    public Rigidbody2D GetStarRigidbody { get => _celestialRigidBody; }
    public Transform GetStarVisualTransform { get => starVisualTransform; }
    public CircleCollider2D GetStarRadiusCollider { get => starRadiusCollider; }
    public Light2D GetStarLight { get => starLight; }

    public void SetRotateSpeedFactor(float factor)
    {
        rotateSpeedFactor = factor;
    }

    protected override void Awake()
    {
        base.Awake();

        _celestialBodyType = CelestialBodyType.Star;

        SetInitialColors();
    }

    protected override void Update()
    {
        UpdateTime(Time.time * rotateSpeedFactor);

        base.Update();
    }

    protected override void UpdateTime(float time)
    {
        _celestialMaterials[0].SetFloat(ShaderProperties.Key_time, time * 0.3f);
        _celestialMaterials[1].SetFloat(ShaderProperties.Key_time, time);
        _celestialMaterials[2].SetFloat(ShaderProperties.Key_time, time);
    }

    protected override void SetPixel(float amount)
    {
        _celestialMaterials[0].SetFloat(ShaderProperties.Key_Pixels, amount);
        _celestialMaterials[1].SetFloat(ShaderProperties.Key_Pixels, amount * 2);
        _celestialMaterials[2].SetFloat(ShaderProperties.Key_Pixels, amount * 2);
    }

    public void SetSeed(float seed)
    {
        float scaledSeed = seed % 1000f / 100f;
        
        for (int i = 0; i < _celestialMaterials.Length; i++)
        {
            _celestialMaterials[0].SetFloat(ShaderProperties.Key_Seed, scaledSeed);
        }

        SetGradientColor(_colors1, _colors2);
    }

    public void SetRotate(float r)
    {
        for (int i = 0; i < _celestialMaterials.Length; i++)
        {
            _celestialMaterials[i].SetFloat(ShaderProperties.Key_Rotation, r);
        }
    }

    public void SetInitialColors()
    {
        SetGradientColor(_colors1, _colors2);

        _celestialMaterials[0].SetColor(colorVars1[0], ColorUtil.FromRGB(initColors1[0]));
    }

    public void SetRandColours()
    {
        string[] colors = new[] { "#ffc59e", "#d69176", "#a6481c", "#5e2903" };
        string[] colors2 = new[] { "#ffc59e", "#f7d9c5" };
        
        SetGradientColor(colors, colors2);
    }

    private void SetGradientColor(string[] colors1, string[] colors2)
    {
        for (int i = 0; i < colorKey1.Length; i++)
        {
            colorKey1[i].color = default(Color);
            ColorUtility.TryParseHtmlString(colors1[i], out colorKey1[i].color);

            colorKey1[i].time = _color_times1[i];
            alphaKey1[i].alpha = 1.0f;
        }

        for (int i = 0; i < colorKey2.Length; i++)
        {
            colorKey2[i].color = default(Color);
            ColorUtility.TryParseHtmlString(colors2[i], out colorKey2[i].color);

            colorKey2[i].time = _color_times2[i];
            alphaKey2[i].alpha = 1.0f;
        }

        Texture2D starTexture = _gradientStar.SetColors(colorKey1, alphaKey1, gradient_vars);
        Texture2D flareTexture = _gradientStarFlare.SetColors(colorKey2, alphaKey2, gradient_vars);

        _celestialMaterials[0].SetTexture(gradient_vars, starTexture);
        _celestialMaterials[1].SetTexture(gradient_vars, flareTexture);
        _celestialMaterials[2].SetTexture(gradient_vars, starTexture);
    }

    public Color[] GetColors()
    {
        Color[] colors = new Color[7];
        GradientColorKey[] gradColors = _gradientStar.GetColorKeys();
        GradientColorKey[]  gradColors2 = _gradientStarFlare.GetColorKeys();
        int size = colorVars1.Length;

        for (int i = 0; i < colorVars1.Length; i++)
        {
            colors[i] = _celestialMaterials[0].GetColor(colorVars1[i]);
        }

        for (int i = 0; i < gradColors.Length; i++)
        {
            colors[i + size] = gradColors[i].color;
        }

        size += gradColors.Length;

        for (int i = 0; i < gradColors2.Length; i++)
        {
            colors[i + size] = gradColors2[i].color;
        }

        return colors;
    }

    public void SetColors(Color[] _colors)
    {
        int size = colorVars1.Length;
        
        for (int i = 0; i < colorVars1.Length; i++)
        {
            _celestialMaterials[2].SetColor(colorVars1[i], _colors[i]);
        }

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