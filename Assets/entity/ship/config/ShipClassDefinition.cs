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

        [Header("Module Slots (Legacy)")]
        [SerializeField] private List<ModuleSlotEntry> moduleSlots = new();

        [Header("Module Slots (Multi-Slot System)")]
        [SerializeField] private List<MultiSlotEntry> multiSlots = new();

        [Header("Category Limits")]
        [Tooltip("Define maximum counts for module categories, subcategories, or specific types")]
        [SerializeField] private List<CategoryLimit> categoryLimits = new();

        [Header("Capabilities")]
        [SerializeField] private EntityCapabilities[] capabilities;

        [Header("Visual")]
        [SerializeField] private GameObject prefabOverride;
        [SerializeField] private float spriteScale = 1f;

        // === Legacy accessors ===
        public string ClassId => classId;
        public string ClassName => className;
        public string Description => description;
        public Sprite ClassIcon => classIcon;
        public IReadOnlyList<ModuleSlotEntry> ModuleSlots => moduleSlots;
        public EntityCapabilities[] Capabilities => capabilities;
        public GameObject PrefabOverride => prefabOverride;
        public float SpriteScale => spriteScale;

        // === Multi-slot accessors ===
        public IReadOnlyList<MultiSlotEntry> MultiSlots => multiSlots;
        public IReadOnlyList<CategoryLimit> CategoryLimits => categoryLimits;

        // === Legacy methods ===

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

        // === Multi-slot methods ===

        /// <summary>
        /// Checks if a slot with the given ID exists.
        /// </summary>
        public bool HasMultiSlot(string slotId)
            => multiSlots.Any(s => s.slotId == slotId);

        /// <summary>
        /// Gets a multi-slot entry by ID.
        /// </summary>
        public MultiSlotEntry GetMultiSlot(string slotId)
            => multiSlots.FirstOrDefault(s => s.slotId == slotId);

        /// <summary>
        /// Gets all multi-slot entries of a specific module type.
        /// </summary>
        public IEnumerable<MultiSlotEntry> GetMultiSlotsByType(ModuleTypeId typeId)
            => multiSlots.Where(s => s.moduleType == typeId);

        /// <summary>
        /// Gets all multi-slot entries in a category.
        /// </summary>
        public IEnumerable<MultiSlotEntry> GetMultiSlotsByCategory(ModuleCategory category)
            => multiSlots.Where(s => ModuleHierarchyRegistry.GetCategory(s.moduleType) == category);

        /// <summary>
        /// Gets all multi-slot entries in a subcategory.
        /// </summary>
        public IEnumerable<MultiSlotEntry> GetMultiSlotsBySubCategory(ModuleSubCategory subCategory)
            => multiSlots.Where(s => ModuleHierarchyRegistry.GetSubCategory(s.moduleType) == subCategory);

        /// <summary>
        /// Builds multi-slot configurations for runtime use.
        /// </summary>
        public MultiSlotConfiguration[] BuildMultiSlotConfigurations()
        {
            return multiSlots.Select(entry => new MultiSlotConfiguration(entry)).ToArray();
        }

        /// <summary>
        /// Counts how many slots of a specific type are defined.
        /// </summary>
        public int CountSlotsOfType(ModuleTypeId typeId)
            => multiSlots.Count(s => s.moduleType == typeId);

        /// <summary>
        /// Counts how many slots are in a category.
        /// </summary>
        public int CountSlotsInCategory(ModuleCategory category)
            => multiSlots.Count(s => ModuleHierarchyRegistry.GetCategory(s.moduleType) == category);

        /// <summary>
        /// Counts how many slots are in a subcategory.
        /// </summary>
        public int CountSlotsInSubCategory(ModuleSubCategory subCategory)
            => multiSlots.Count(s => ModuleHierarchyRegistry.GetSubCategory(s.moduleType) == subCategory);

        /// <summary>
        /// Validates that the slot configuration doesn't exceed any category limits.
        /// </summary>
        public bool ValidateLimits(out string errorMessage)
        {
            foreach (var limit in categoryLimits)
            {
                int count = limit.scope switch
                {
                    CategoryLimit.LimitScope.Category =>
                        CountSlotsInCategory(limit.category),
                    CategoryLimit.LimitScope.SubCategory =>
                        CountSlotsInSubCategory(limit.subCategory),
                    CategoryLimit.LimitScope.ModuleType =>
                        CountSlotsOfType(limit.moduleType),
                    _ => 0
                };

                if (count > limit.maxCount)
                {
                    errorMessage = $"Exceeded {limit.GetDescription()}: {count}/{limit.maxCount}";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Gets the limit for a specific scope, or null if no limit is defined.
        /// </summary>
        public CategoryLimit GetLimitFor(ModuleCategory category)
            => categoryLimits.FirstOrDefault(l =>
                l.scope == CategoryLimit.LimitScope.Category && l.category == category);

        public CategoryLimit GetLimitFor(ModuleSubCategory subCategory)
            => categoryLimits.FirstOrDefault(l =>
                l.scope == CategoryLimit.LimitScope.SubCategory && l.subCategory == subCategory);

        public CategoryLimit GetLimitFor(ModuleTypeId typeId)
            => categoryLimits.FirstOrDefault(l =>
                l.scope == CategoryLimit.LimitScope.ModuleType && l.moduleType == typeId);

        public Ship CreateShip()
        {
            return new Ship(this);
        }

        private void OnValidate()
        {
            // Validate legacy slots
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

            // Validate multi-slot unique IDs
            var duplicateIds = multiSlots
                .GroupBy(s => s.slotId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicateId in duplicateIds)
            {
                Debug.LogWarning($"[{name}] Duplicate slot ID: '{duplicateId}'");
            }

            // Validate category limits
            if (!ValidateLimits(out string limitError))
            {
                Debug.LogWarning($"[{name}] {limitError}");
            }
        }
    }
}
