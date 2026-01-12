using UnityEngine;

namespace Starfire.Entity.Modules.Rotation
{
    [CreateAssetMenu(fileName = "SmoothRotation", menuName = "Starfire/Modules/Rotation/Smooth")]
    public class SmoothRotationConfig : RotationModuleConfig
    {
        [Header("Smooth Rotation Settings")]
        [Tooltip("Smoothing factor (higher = faster response)")]
        [Range(1f, 20f)]
        [SerializeField] private float smoothingFactor = 8f;

        [Tooltip("Minimum angle change to apply rotation (deadzone)")]
        [SerializeField] private float deadzone = 0.5f;

        [Tooltip("Use SmoothDamp instead of Lerp for more natural feel")]
        [SerializeField] private bool useSmoothDamp = true;

        public float SmoothingFactor => smoothingFactor;
        public float Deadzone => deadzone;
        public bool UseSmoothDamp => useSmoothDamp;

        public override IRotationModule CreateModule()
        {
            return new SmoothRotationModule(this);
        }
    }
}
