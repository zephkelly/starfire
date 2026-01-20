using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject that provides access to the global input provider.
    /// Assign this to PlayerEntityControllerDriver in the inspector.
    /// This bridges editor-time configuration with runtime input provider lookup.
    /// </summary>
    [CreateAssetMenu(fileName = "InputProviderReference", menuName = "Starfire/Input/Input Provider Reference")]
    public class InputProviderReference : ScriptableObject
    {
        [SerializeField, Tooltip("Tag to find the input provider GameObject at runtime (fallback if registry not found)")]
        private string providerTag = "InputProvider";

        [SerializeField, Tooltip("If true, cache the provider after first lookup")]
        private bool cacheProvider = true;

        private IInputProvider cachedProvider;
        private bool hasLookedUp;

        public IInputProvider GetProvider()
        {
            if (cacheProvider && hasLookedUp)
                return cachedProvider;

            // Try to get from registry first (faster, preferred)
            if (InputProviderRegistry.Instance != null)
            {
                cachedProvider = InputProviderRegistry.Instance.GetProvider();
                hasLookedUp = true;
                return cachedProvider;
            }

            // Fallback: find by tag
            if (!string.IsNullOrEmpty(providerTag))
            {
                var providerGO = GameObject.FindGameObjectWithTag(providerTag);
                if (providerGO != null)
                {
                    cachedProvider = providerGO.GetComponent<IInputProvider>();
                }
            }

            hasLookedUp = true;
            return cachedProvider;
        }

        public void ClearCache()
        {
            cachedProvider = null;
            hasLookedUp = false;
        }

        private void OnDisable()
        {
            ClearCache();
        }
    }
}
