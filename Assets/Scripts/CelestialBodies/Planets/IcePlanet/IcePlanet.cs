using UnityEngine;

namespace Starfire
{
  public class IceWorld : MonoBehaviour, IPlanet
  {
    // public OrbitingController OrbitController { get; private set; }
    // public PlanetType PlanetType { get; private set; }

    // public float MaxOrbitRadius { get; private set; }
    // public float Temperature { get; private set; }
    // public Vector2 GetWorldPosition() => transform.position;

    private string[] color_vars1 = new string[]{"_Color1", "_Color2", "_Color3"};
    private string[] init_colors1 = new string[] {"#faffff", "#c7d4e1", "#928fb8"};
    
    private string[] color_vars2 = new string[]{"_Color1", "_Color2", "_Color3"};
    private string[] init_colors2 = new string[] {"#4fa4b8", "#4c6885", "#3a3f5e"};
    
    private string[] color_vars3 = new string[]{"_Base_color", "_Outline_color", "_Shadow_Base_color","_Shadow_Outline_color"};
    private string[] init_colors3 = new string[] {"#e1f2ff", "#c0e3ff", "#5e70a5","#404973"};
  }
}