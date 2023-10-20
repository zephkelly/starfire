using UnityEngine;

namespace Starfire.Items
{
  public interface IItem
  {
    string Name { get; }
    string Description { get; }
    Sprite Icon { get; }
  }

  public enum ItemType
  {
    AsteroidPickup,
    ShipPart,
    Upgrade,
    Consumable,
    Artifact,
  }

  public class ItemBase
  {
    
  }
}