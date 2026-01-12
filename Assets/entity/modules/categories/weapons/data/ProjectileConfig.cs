using System;
using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    [Serializable]
    public class ProjectileConfig
    {
        [Header("Movement")]
        [Tooltip("Projectile speed in units per second")]
        public float speed = 20f;

        [Tooltip("Maximum lifetime in seconds before auto-destroy")]
        public float lifetime = 3f;

        [Tooltip("If true, projectile inherits ship velocity")]
        public bool inheritVelocity = true;

        [Header("Collision")]
        [Tooltip("Layer mask for collision detection")]
        public LayerMask hitLayers;

        [Tooltip("If true, projectile is destroyed on first hit")]
        public bool destroyOnHit = true;

        [Header("Visual")]
        [Tooltip("Scale of the projectile sprite")]
        public float scale = 1f;

        [Tooltip("Color tint for the projectile")]
        public Color color = Color.white;
    }
}
