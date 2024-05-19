using UnityEngine;

namespace Starfire
{
    public class StarParallaxLayer : MonoBehaviour, IParallaxLayer
    {
        [SerializeField] private Transform sprite; 
        [SerializeField] private float parallaxFactor = 1f;

        private Transform cameraTransform;

        private void Awake()
        {
            cameraTransform = Camera.main.transform;
        }

        public void Parallax(Vector3 cameraLastPosition)
        {
            Vector3 cameraDelta = (Vector2)(cameraTransform.position - cameraLastPosition);

            sprite.position = Vector3.Lerp(
                sprite.position, 
                sprite.position + cameraDelta, 
                parallaxFactor * Time.deltaTime
            );

        }
    }
}