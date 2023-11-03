using UnityEngine;

namespace Starfire
{
  public class Asteroid : MonoBehaviour 
  {
    public OrbitingController OrbitController { get; private set; }
    public CelestialBodyType CelestialBodyType { get; private set; }

    public ICelestialBody ParentOrbitingBody { get; private set; }
    public ICelestialBody ChildOrbitingBody { get; private set; }
    public float MaxOrbitRadius { get; private set; }
    public float Temperature { get; private set; }
    public bool IsOrbiting => ParentOrbitingBody != null;
    public Vector2 GetWorldPosition() => transform.position;

    [SerializeField] private GameObject asteroidObject;
    private Material m_Asteroid;

    [SerializeField] private short seed;

    private string[] color_vars = new string[]{"_Color1", "_Color2", "_Color3"};
    private string[] init_colors = new string[] {"#a3a7c2", "#4c6885", "#3a3f5e"};

    public void SetCelestialBodyType(CelestialBodyType type) => CelestialBodyType = type;

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
        m_Asteroid = asteroidObject.GetComponent<SpriteRenderer>().material;
        SetInitialColors();
    }

    public void SetPixel(float amount)
    {
        m_Asteroid.SetFloat(ShaderProperties.Key_Pixels, amount);
    }

    public void SetLight(Vector2 pos)
    {
        m_Asteroid.SetVector(ShaderProperties.Key_Light_origin, pos);
    }

    public void SetSeed(float seed)
    {
      var converted_seed = seed % 1000f / 100f;
      m_Asteroid.SetFloat(ShaderProperties.Key_Seed, converted_seed);
    }

    public void SetRotate(float r)
    {
        m_Asteroid.SetFloat(ShaderProperties.Key_Rotation, r);
    }

    public void UpdateTime(float time)
    {
        return;
    }

    public void SetCustomTime(float time)
    {
        var dt = time * 6.28f;
        time = Mathf.Clamp(dt,0.1f, 6.28f);
        m_Asteroid.SetFloat(ShaderProperties.Key_Rotation, time);
    }
    public void SetInitialColors()
    {
        for (int i = 0; i < color_vars.Length; i++)
        {
            m_Asteroid.SetColor(color_vars[i], ColorUtil.FromRGB(init_colors[i]));
        }
    }
    public Color[] GetColors()
    {
        var colors = new Color[3];
        for (int i = 0; i < color_vars.Length; i++)
        {
            colors[i] = m_Asteroid.GetColor(color_vars[i]);
        }
        return colors;
    }

    public void SetColors(Color[] _colors)
    {
        for (int i = 0; i < _colors.Length; i++)
        {
            m_Asteroid.SetColor(color_vars[i], _colors[i]);
        }
    }
  }
}