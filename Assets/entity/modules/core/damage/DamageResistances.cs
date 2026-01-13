using UnityEngine;

namespace Starfire.Entity.Modules.Damage
{
    /// <summary>
    /// Configurable resistance values for each damage type.
    /// Can be used by both shields and hulls.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageResistances", menuName = "Starfire/Damage/Resistances")]
    public class DamageResistances : ScriptableObject
    {
        [Header("Resistance Values (0-1, where 1 = immune)")]
        [Range(0f, 1f)]
        [SerializeField] private float kinetic = 0f;

        [Range(0f, 1f)]
        [SerializeField] private float energy = 0f;

        [Range(0f, 1f)]
        [SerializeField] private float explosive = 0f;

        [Range(0f, 1f)]
        [SerializeField] private float electromagnetic = 0f;

        [Range(0f, 1f)]
        [SerializeField] private float plasma = 0f;

        /// <summary>
        /// Gets the resistance value for a specific damage type.
        /// </summary>
        public float GetResistance(DamageType type)
        {
            return type switch
            {
                DamageType.Kinetic => kinetic,
                DamageType.Energy => energy,
                DamageType.Explosive => explosive,
                DamageType.Electromagnetic => electromagnetic,
                DamageType.Plasma => plasma,
                DamageType.True => 0f,
                _ => 0f
            };
        }

        /// <summary>
        /// Calculates the damage multiplier (1 - resistance).
        /// A resistance of 0.3 results in a multiplier of 0.7 (70% damage taken).
        /// </summary>
        public float GetDamageMultiplier(DamageType type)
        {
            return 1f - GetResistance(type);
        }
    }
}
