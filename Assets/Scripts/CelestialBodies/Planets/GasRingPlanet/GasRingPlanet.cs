using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfire
{
  public class GasLayers : MonoBehaviour, ICelestialBody, IPlanet
  {
    public OrbitingController OrbitController { get; private set; }
    public CelestialBodyType CelestialBodyType { get; private set; }
    public PlanetType PlanetType { get; private set; }

    public ICelestialBody ParentOrbitingBody { get; private set; }
    public ICelestialBody ChildOrbitingBody { get; private set; }
    public float MaxOrbitRadius { get; private set; }
    public float Temperature { get; private set; }
    public bool IsOrbiting => ParentOrbitingBody != null;
    public Vector2 GetWorldPosition() => transform.position;

    [SerializeField] private GameObject _GasLayers;
    [SerializeField] private GameObject _Ring;
    
    [SerializeField] private GradientTextureGenerate _gradientGas1;
    [SerializeField] private GradientTextureGenerate _gradientGas2;
    [SerializeField] private GradientTextureGenerate _gradientRing1;
    [SerializeField] private GradientTextureGenerate _gradientRing2;

    private Material m_GasLayers;
    private Material m_Ring;
    private string gradient_vars = "_ColorScheme";
    private string gradient_dark_vars = "_Dark_ColorScheme";
    private GradientColorKey[] colorKey1 = new GradientColorKey[3];
    private GradientColorKey[] colorKey2 = new GradientColorKey[3];
    
    private GradientAlphaKey[] alphaKey = new GradientAlphaKey[3];
    
    private string[] _colors1 = new[] {"#eec39a", "#d9a066", "#8f563b"};
    private string[] _colors2 = new[] {"#663931", "#45283c", "#222034"};
    private float[] _color_times = new float[] { 0, 0.5f, 1.0f };

    public void SetCelestialBodyType(CelestialBodyType type) => CelestialBodyType = type;
    public void SetPlanetType(PlanetType type) => PlanetType = type;

    public void SetOrbitingBody(ICelestialBody _parentOrbitingBody)
    {
      ParentOrbitingBody = _parentOrbitingBody;
    }

    public void RemoveOrbitingBody()
    {
      ParentOrbitingBody = null;
    }

      private void Awake()
      {
          m_GasLayers = _GasLayers.GetComponent<SpriteRenderer>().material;
          m_Ring = _Ring.GetComponent<SpriteRenderer>().material;
          setGragientColor();
      }
      public void SetPixel(float amount)
      {
          m_GasLayers.SetFloat(ShaderProperties.Key_Pixels, amount);
          m_Ring.SetFloat(ShaderProperties.Key_Pixels, amount * 3f);
      }

      public void SetLight(Vector2 pos)
      {
          m_GasLayers.SetVector(ShaderProperties.Key_Light_origin, pos * 1.3f  );
          m_Ring.SetVector(ShaderProperties.Key_Light_origin, pos * 1.3f );
      }

      public void SetSeed(float seed)
      {
          var converted_seed = seed % 1000f / 100f;
          m_GasLayers.SetFloat(ShaderProperties.Key_Seed, converted_seed);
          m_Ring.SetFloat(ShaderProperties.Key_Seed, converted_seed);
        //  _Ring.SetFloat("cloud_cover", Random.Range(0.28f, 0.5f));
      }

      public void SetRotate(float r)
      {
          m_GasLayers.SetFloat(ShaderProperties.Key_Rotation, r);
          m_Ring.SetFloat(ShaderProperties.Key_Rotation, r + 0.7f);
      }

      public void UpdateTime(float time)
      {
          m_GasLayers.SetFloat(ShaderProperties.Key_time, time * 0.5f);
          m_Ring.SetFloat(ShaderProperties.Key_time, time  * 0.5f * -3f);
      }

      public void SetCustomTime(float time)
      {
          var dt = 10f + time * 60f;
          m_GasLayers.SetFloat(ShaderProperties.Key_time, dt * 0.5f);
          m_Ring.SetFloat(ShaderProperties.Key_time, dt * 0.5f * -3f);
      }
      public void SetInitialColors()
      {
          setGragientColor();
      }

      private void setGragientColor()
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

          m_GasLayers.SetTexture(gradient_vars, gasTexture);
          m_GasLayers.SetTexture(gradient_dark_vars, gasTextureDark);

          m_Ring.SetTexture(gradient_vars, ringTexture);
          m_Ring.SetTexture(gradient_dark_vars, ringTextureDark);
      }
      public Color[] GetColors()
      {
          var colors = new Color[12];
          var gradColors = _gradientGas1.GetColorKeys();
          for (int i = 0; i < gradColors.Length; i++)
          {
              colors[i] = gradColors[i].color;
          }
          var size = gradColors.Length;
          
          var gradColors2 = _gradientGas2.GetColorKeys();
          for (int i = 0; i < gradColors2.Length; i++)
          {
              colors[i + size] = gradColors2[i].color;
          }

          size += gradColors2.Length;
          
          var gradColors3 = _gradientRing1.GetColorKeys();
          for (int i = 0; i < gradColors3.Length; i++)
          {
              colors[i + size] = gradColors3[i].color;
          }

          size += gradColors3.Length;
          
          var gradColors4 = _gradientRing2.GetColorKeys();
          for (int i = 0; i < gradColors4.Length; i++)
          {
              colors[i + size] = gradColors4[i].color;
          }
          
          return colors;
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