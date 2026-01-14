using UnityEngine;

namespace Starfire.Entity
{
    [RequireComponent(typeof(EntityControllerBase))]
    public class EntityRegistryHook : MonoBehaviour
    {
        private EntityControllerBase _controller;

        private void Awake()
        {
            _controller = GetComponent<EntityControllerBase>();
        }

        private void OnEnable()
        {
            if (EntityRegistry.Instance != null)
            {
                EntityRegistry.Instance.Register(_controller);
            }
        }

        private void OnDisable()
        {
            if (EntityRegistry.Instance != null)
            {
                EntityRegistry.Instance.Unregister(_controller);
            }
        }
    }
}
