using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Starfire.Entity.Modules;

namespace Starfire.Entity
{
    [CreateAssetMenu(fileName = "NewShipClass", menuName = "Starfire/Ship/Ship Class Definition")]
    public class ShipClassDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string classId;
        [SerializeField] private string className;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite classIcon;

        [Header("Module Slots")]
        [SerializeField] private List<ModuleSlotEntry> moduleSlots = new();

        [Header("Capabilities")]
        [SerializeField] private EntityCapabilities[] capabilities;

        [Header("Visual")]
        [SerializeField] private GameObject prefabOverride;
        [SerializeField] private float spriteScale = 1f;

        public string ClassId => classId;
        public string ClassName => className;
        public string Description => description;
        public Sprite ClassIcon => classIcon;
        public IReadOnlyList<ModuleSlotEntry> ModuleSlots => moduleSlots;
        public EntityCapabilities[] Capabilities => capabilities;
        public GameObject PrefabOverride => prefabOverride;
        public float SpriteScale => spriteScale;

        public bool HasSlot(ModuleSlotType type)
            => moduleSlots.Any(s => s.slotType == type);

        public ModuleSlotEntry GetSlot(ModuleSlotType type)
            => moduleSlots.FirstOrDefault(s => s.slotType == type);

        public SlotConfiguration[] BuildSlotConfigurations()
        {
            return moduleSlots.Select(entry => new SlotConfiguration
            {
                slotType = entry.slotType,
                isAvailable = true,
                isRequired = entry.isRequired,
                defaultModule = entry.defaultModule
            }).ToArray();
        }

        public Ship CreateShip()
        {
            return new Ship(this);
        }

        private void OnValidate()
        {
            foreach (var slot in moduleSlots)
            {
                if (slot.defaultModule != null &&
                    !ModuleTypeRegistry.IsValidConfigForSlot(slot.slotType, slot.defaultModule))
                {
                    var expectedType = ModuleTypeRegistry.GetConfigTypeForSlot(slot.slotType);
                    Debug.LogWarning(
                        $"[{name}] Invalid module config for {slot.slotType} slot. " +
                        $"Expected {expectedType?.Name}, got {slot.defaultModule.GetType().Name}");
                }
            }
        }
    }
}
