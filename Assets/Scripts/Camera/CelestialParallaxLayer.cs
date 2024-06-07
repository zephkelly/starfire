using UnityEngine;

namespace Starfire
{
    public class CelestialParallaxLayer : MonoBehaviour, IParallaxLayer
    {
        [SerializeField] private Transform starTransform;
        [SerializeField] private Transform sprite; 
        [SerializeField] private float parallaxFactor = 1f;

        private Transform playerTransform;
        private Transform cameraTransform;

        private Vector2 starPosition;

        private void Awake()
        {
            cameraTransform = Camera.main.transform;
            playerTransform = GameObject.Find("PlayerShip").transform;
        }


        public void Parallax(Vector3 cameraLastPosition)
        {
            if (starTransform == null) return;
            
            Vector2 initialPosition = (Vector2)starTransform.position;

            Vector2 cameraDelta = (Vector2)cameraTransform.position - initialPosition;
            Vector2 parallaxPosition = initialPosition + cameraDelta * parallaxFactor;

            sprite.position = parallaxPosition;  
        }

        public void SetParallaxFactor(float _parallaxFactor)
        {
            parallaxFactor = _parallaxFactor;
        }
    }
}