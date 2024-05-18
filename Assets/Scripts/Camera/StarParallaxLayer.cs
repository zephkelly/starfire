using UnityEngine;

namespace Starfire
{
    public class StarParallaxLayer : ParallaxLayer
    {
        [SerializeField] private float parallaxFactor;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Parallax(Vector3 cameraLastPosition, float pf = 1f, Transform obj = null)
        {
            base.Parallax(cameraLastPosition, parallaxFactor, transform);
        }
    }
}