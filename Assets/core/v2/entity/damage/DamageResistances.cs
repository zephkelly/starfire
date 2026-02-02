using UnityEngine;

namespace StarfireV2
{
    [CreateAssetMenu(fileName = "DamageResistances", menuName = "StarfireV2/Damage/Resistances")]
    public class DamageResistances : ScriptableObject
    {
        [Header("Resistance Values (0-1, where 1 = immune)")]
        [Range(0f, 1f), SerializeField] private float kinetic = 0f;
        [Range(0f, 1f), SerializeField] private float energy = 0f;
        [Range(0f, 1f), SerializeField] private float explosive = 0f;
        [Range(0f, 1f), SerializeField] private float thermal = 0f;
        [Range(0f, 1f), SerializeField] private float em = 0f;

        public float GetResistance(V2DamageType type)
        {
            return type switch
            {
                V2DamageType.Kinetic => kinetic,
                V2DamageType.Energy => energy,
                V2DamageType.Explosive => explosive,
                V2DamageType.Thermal => thermal,
                V2DamageType.EM => em,
                _ => 0f
            };
        }

        public float GetDamageMultiplier(V2DamageType type)
        {
            return 1f - GetResistance(type);
        }
    }
}
