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
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }


        public void Parallax(Vector3 cameraLastPosition)
        {
            if (starTransform == null) return;

            Vector2 initialPosition = new Vector2(starTransform.position.x, starTransform.position.y);
            Vector2 cameraPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.y);
            Vector2 cameraDelta = cameraPosition - initialPosition;
            Vector2 parallaxPosition = initialPosition + cameraDelta * parallaxFactor;

            sprite.position = parallaxPosition;  
        }

        public void SetParallaxFactor(float _parallaxFactor)
        {
            parallaxFactor = _parallaxFactor;
        }
    }
}