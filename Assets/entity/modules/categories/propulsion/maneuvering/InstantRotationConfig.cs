using UnityEngine;

namespace Starfire.Entity.Modules.Rotation
{
    [CreateAssetMenu(fileName = "InstantRotation", menuName = "Starfire/Modules/Rotation/Instant")]
    public class InstantRotationConfig : RotationModuleConfig
    {
        [Header("Instant Rotation Settings")]
        [Tooltip("If true, still respects max rotation speed. If false, truly instant.")]
        [SerializeField] private bool respectMaxSpeed = false;

        public bool RespectMaxSpeed => respectMaxSpeed;

        public override IRotationModule CreateModule()
        {
            return new InstantRotationModule(this);
        }
    }
}
