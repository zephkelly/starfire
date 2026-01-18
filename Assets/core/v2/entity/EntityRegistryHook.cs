using UnityEngine;

namespace StarfireV2
{
    [RequireComponent(typeof(IEntityController))]
    public class EntityRegistryHook : MonoBehaviour
    {
        private IEntityController _controller;
        private bool _isRegistered;

        private void Awake()
        {
            _controller = GetComponent<IEntityController>();
        }

        private void Start()
        {
            // Register in Start() instead of OnEnable() to ensure Systems are initialized
            // AISetup/PlayerSetup.Start() calls Initialize() which creates Systems
            TryRegister();
        }

        private void OnEnable()
        {
            if (_isRegistered)
            {
                TryRegister();
            }
        }

        private void TryRegister()
        {
            if (EntityRegistry.Instance != null && _controller.Entity != null)
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