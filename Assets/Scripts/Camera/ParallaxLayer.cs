using UnityEngine;

namespace Starfire
{
    public interface IParallaxLayer
    {
        void Parallax(Transform obj, Vector3 cameraLastPosition, float parallaxFactor = 1f);
    }

    public abstract class ParallaxLayer : MonoBehaviour, IParallaxLayer
    {
        protected Transform cameraTransform;

        protected virtual void Awake()
        {
            cameraTransform = Camera.main.transform;
        }

        public virtual void Parallax(Transform obj, Vector3 cameraLastPosition, float parallaxFactor = 1f)
        {
            Vector3 cameraDelta = (Vector2)(cameraTransform.position - cameraLastPosition);

            obj.position = Vector3.Lerp(
                obj.position, 
                obj.position - cameraDelta, 
                parallaxFactor * Time.deltaTime
            );
        }
    }
}