using UnityEngine;

namespace Starfire
{
    [RequireComponent(typeof(AlertManager))]
    [RequireComponent(typeof(HUDManager))]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        
        [SerializeField] private AlertManager alertManager;
        [SerializeField] private HUDManager hudManager;
        [SerializeField] private DialogueManager dialogueManager;

        public AlertManager AlertManager => alertManager;
        public HUDManager HUDManager => hudManager;
        public DialogueManager DialogueManager => dialogueManager;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            alertManager = GetComponent<AlertManager>();
            hudManager = GetComponent<HUDManager>();
            dialogueManager = GetComponent<DialogueManager>();
        }

        public void DisplayMinorAlert(string alertText)
        {
            alertManager.DisplayMinorAlert(alertText);
        }

        public void UpdateHealthBar(float health, float maxHealth)
        {
            HUDManager.UpdateHealthBar(health, maxHealth);
        }

        public void DisplayDialogue(Message dialogueText)
        {
            DialogueManager.DisplayMessage(dialogueText);
        }

        public void HideUI()
        {
            AlertManager.Hide();
            HUDManager.Hide();
            DialogueManager.Hide();
        }

        public void ShowUI()
        {
            AlertManager.Show();
            HUDManager.Show();
            DialogueManager.Show();
        }
    }
}