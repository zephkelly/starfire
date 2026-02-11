using UnityEngine;

namespace Starfire.Entity
{
    [System.Serializable]
    public class AsteroidTypeDefinition
    {
        public string Name;
        public Mesh Mesh;
        public Material Material;
        public float ColliderRadius = 1f;
        public float MinSize;
        public float MaxSize;
        public byte Composition = 255;
    }
}
