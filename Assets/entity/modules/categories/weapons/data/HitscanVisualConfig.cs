using System;
using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Configuration for hitscan weapon visual effects.
    /// Controls how the instant beam/trail appears and fades.
    /// </summary>
    [Serializable]
    public class HitscanVisualConfig
    {
        [Header("Beam Appearance")]
        [Tooltip("Color of the beam")]
        public Color beamColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Tooltip("Width of the beam at the start (muzzle)")]
        public float beamWidth = 0.1f;

        [Tooltip("Ratio of end width to start width (0.5 = half as wide at the end)")]
        [Range(0f, 1f)]
        public float endWidthRatio = 0.5f;

        [Tooltip("Material for the beam. Uses Sprites/Default if null.")]
        public Material beamMaterial;

        [Header("Timing")]
        [Tooltip("How long the beam visual persists before fully fading")]
        public float fadeDuration = 0.15f;

        [Header("Effects")]
        [Tooltip("If true, beam width shrinks as it fades")]
        public bool shrinkOnFade = true;

        [Tooltip("Sorting order for the LineRenderer")]
        public int sortingOrder = 10;
    }
}
