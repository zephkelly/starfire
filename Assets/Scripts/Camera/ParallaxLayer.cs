using UnityEngine;

namespace Starfire
{
    public interface IParallaxLayer
    {
        void Parallax(Vector3 cameraLastPosition, float parallaxFactor = 1f, Transform obj = null);
    }

    public abstract class ParallaxLayer : MonoBehaviour, IParallaxLayer
    {
        protected Transform cameraTransform;

        protected virtual void Awake()
        {
            cameraTransform = Camera.main.transform;
        }

        public virtual void Parallax(Vector3 cameraLastPosition, float parallaxFactor = 1f, Transform obj = null)
        {
            Vector3 cameraDelta = (Vector2)(cameraTransform.position - cameraLastPosition);

            obj.position = Vector3.Lerp(
                obj.position, 
                obj.position + cameraDelta, 
                parallaxFactor * Time.deltaTime
            );
        }
    }
}