using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for an impact light flash effect.
    /// </summary>
    [Serializable]
    public class ImpactLightConfig
    {
        public Color color = Color.white;

        [Range(0f, 5f)]
        public float intensity = 1.5f;

        [Range(0f, 10f)]
        public float radius = 3f;

        [Range(0.05f, 1f)]
        public float duration = 0.15f;

        public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }
}
