using UnityEngine;

namespace Starfire
{
    public interface IItem
    {
        string Name { get; }
        string Description { get; }
        Sprite Icon { get; }
    }

    public enum ItemType
    {
        Part,
        Upgrade,
        Consumable,
        Artifact,
    }

    public class ItemBase
    {
        
    }
}