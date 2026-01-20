using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Global registry for the active input provider.
    /// Place this component on the same GameObject as your input provider (e.g., OldInputProvider).
    /// All PlayerEntityControllerDrivers will automatically discover and use this provider.
    /// </summary>
    public class InputProviderRegistry : MonoBehaviour
    {
        public static InputProviderRegistry Instance { get; private set; }

        [SerializeField, Tooltip("If null, will try to find IInputProvider on this GameObject")]
        private MonoBehaviour inputProviderComponent;

        private IInputProvider registeredProvider;

        public IInputProvider GetProvider() => registeredProvider;

        public void Register(IInputProvider provider)
        {
            registeredProvider = provider;
        }

        public void Unregister(IInputProvider provider)
        {
            if (registeredProvider == provider)
                registeredProvider = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[InputProviderRegistry] Duplicate instance detected, destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Auto-register if component is set or found on this GameObject
            if (inputProviderComponent != null && inputProviderComponent is IInputProvider provider)
            {
                registeredProvider = provider;
            }
            else
            {
                var foundProvider = GetComponent<IInputProvider>();
                if (foundProvider != null)
                {
                    registeredProvider = foundProvider;
                }
            }

            if (registeredProvider == null)
            {
                Debug.LogWarning("[InputProviderRegistry] No IInputProvider found. " +
                    "Add an input provider component to this GameObject or assign one manually.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
