using UnityEngine;

namespace Starfire.Entity
{
    [RequireComponent(typeof(EntityControllerBase))]
    public class EntityRegistryHook : MonoBehaviour
    {
        private EntityControllerBase _controller;
        private bool _isRegistered;

        private void Awake()
        {
            _controller = GetComponent<EntityControllerBase>();
        }

        private void Start()
        {
            // Register in Start() instead of OnEnable() to ensure Systems are initialized
            // AISetup/PlayerSetup.Start() calls Initialize() which creates Systems
            TryRegister();
        }

        private void OnEnable()
        {
            // Only re-register if we were previously registered (object re-enabled after disable)
            if (_isRegistered)
            {
                TryRegister();
            }
        }

        private void TryRegister()
        {
            if (EntityRegistry.Instance != null && _controller.Systems != null)
            {
                EntityRegistry.Instance.Register(_controller);
                _isRegistered = true;
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
