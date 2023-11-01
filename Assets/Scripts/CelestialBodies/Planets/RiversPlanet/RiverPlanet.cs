using UnityEngine;

namespace Starfire
{
  public class RiverPlanet : MonoBehaviour, ICelestialBody, IPlanet
  {
    public OrbitingController OrbitingController { get; private set; }
    public CelestialBodyType CelestialBodyType { get; private set; }
    public PlanetType PlanetType { get; private set; }

    [SerializeField] private GameObject Land;
    [SerializeField] private GameObject Cloud;

    private Material m_Land;
    private Material m_Cloud;

    private string[] color_vars1 = new string[]{"_Color1", "_Color2", "_Color3","_Color4","_River_color","_River_color_dark"};
    private string[] init_colors1 = new string[] {"#63AB3F", "#3B7D4F", "#2F5753", "#283540", "#4FA4B8", "#404973"};
    private string[] color_vars2 = new string[]{"_Base_color", "_Outline_color", "_Shadow_Base_color","_Shadow_Outline_color"};
    private string[] init_colors2 = new string[] {"#FFFFFF", "#DFE0E8", "#686F99","#404973"};

    public void SetCelestialBodyType(CelestialBodyType type) => CelestialBodyType = type;
    public void SetPlanetType(PlanetType type) => PlanetType = type;

    private void Awake()
    {
      m_Land = Land.GetComponent<SpriteRenderer>().material;
      m_Cloud = Cloud.GetComponent<SpriteRenderer>().material;
      SetInitialColors();
    }

    public void SetPixel(float amount)
    {
      m_Land.SetFloat(ShaderProperties.Key_Pixels, amount);
      m_Cloud.SetFloat(ShaderProperties.Key_Pixels, amount);
    }

    public void SetLight(Vector2 pos)
    {
      m_Land.SetVector(ShaderProperties.Key_Light_origin, pos);
      m_Cloud.SetVector(ShaderProperties.Key_Light_origin, pos);
    }

    public void SetSeed(float seed)
    {
      var converted_seed = seed % 1000f / 100f;

      m_Land.SetFloat(ShaderProperties.Key_Seed, converted_seed);
      m_Cloud.SetFloat(ShaderProperties.Key_Seed, converted_seed);
      m_Cloud.SetFloat(ShaderProperties.Key_Cloud_cover, Random.Range(0.35f, 0.6f));
    }

    public void SetRotate(float r)
    {
      m_Land.SetFloat(ShaderProperties.Key_Rotation, r);
      m_Cloud.SetFloat(ShaderProperties.Key_Rotation, r);
    }

    public void UpdateTime(float time)
    {
      m_Cloud.SetFloat(ShaderProperties.Key_time, time * 0.25f);
      m_Land.SetFloat(ShaderProperties.Key_time, time * 0.5f);
    }

    public void SetCustomTime(float time)
    {
      var dt = 10f + time * 60f;

      m_Cloud.SetFloat(ShaderProperties.Key_time, dt * 0.25f );
      m_Land.SetFloat(ShaderProperties.Key_time, dt * 0.5f);
    }
    
    public void SetInitialColors()
    {
      for (int i = 0; i < color_vars1.Length; i++)
      {
        m_Land.SetColor(color_vars1[i], ColorUtil.FromRGB(init_colors1[i]));
      }
      for (int i = 0; i < color_vars2.Length; i++)
      {
        m_Cloud.SetColor(color_vars2[i], ColorUtil.FromRGB(init_colors2[i]));
      }
    }
    public Color[] GetColors()
    {
      var colors = new Color[10];
      int pos = 0;

      for (int i = 0; i < color_vars1.Length; i++)
      {
        colors[i] = m_Land.GetColor(color_vars1[i]);
      }

      pos = color_vars1.Length;

      for (int i = 0; i < color_vars2.Length; i++)
      {
        colors[i + pos] = m_Cloud.GetColor(color_vars2[i]);
      }

      return colors;
    }

    public void SetColors(Color[] _colors)
    {
      for (int i = 0; i < color_vars1.Length; i++)
      {
        m_Land.SetColor(color_vars1[i], _colors[i]);
      }
      for (int i = 0; i < color_vars2.Length; i++)
      {
        m_Cloud.SetColor(color_vars2[i], _colors[i + color_vars1.Length]);
      }
    }
  }
}