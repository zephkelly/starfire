using UnityEngine;
using UnityEngine.UI;

namespace Starfire
{
    public class HUDManager : MonoBehaviour, IUIComponent
    {
        [SerializeField] private Canvas healthbarCanvas;
        [SerializeField] private Image healthbarFill;

        [SerializeField] private Canvas warpFuelCanvas;
        [SerializeField] private Image warpFuelFill;

        private void Awake()
        {
            if (healthbarCanvas == null)
            {
                Debug.LogError("Healthbar canvas not set in HUDManager");
            }

            if (healthbarFill == null)
            {
                Debug.LogError("Healthbar fill not set in HUDManager");
            }
        }

        
        private void Update()
        {

        }

        public void Show()
        {
            throw new System.NotImplementedException();
        }

        public void Hide()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateHealthBar(float health, float maxHealth)
        {
            healthbarFill.fillAmount = health / maxHealth;
        }

        public void UpdateWarpFuelBar(float warpFuel, float maxWarpFuel)
        {
            warpFuelFill.fillAmount = warpFuel / maxWarpFuel;
        }
    }
}