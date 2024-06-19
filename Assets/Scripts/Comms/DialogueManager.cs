using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Starfire
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance;

        [SerializeField] private TextMeshProUGUI dialogueText;

        Queue<Message> dialogueQueue = new Queue<Message>();
        private bool isDisplayingDialogue = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // var newSender = new Transponder("Scavenger", FactionType.Scavenger, "90000Hz");
            // var newReceiver = new Transponder("Player", FactionType.Player, "90000Hz");

            // var newMessage = new Message(newSender, newReceiver, "Hello young new traveller! First time in this sector? I'm here to help you out!");
            // DisplayMessage(newMessage);

            // var newMessage2 = new Message(newSender, newReceiver, "Just give me all your loot and turn off your ship, that would be great!");
            // DisplayMessage(newMessage2);

            // var newMessage3 = new Message(newReceiver, newReceiver, "Ignore him, these scavengers are nothing more that little pests running around the system.");
            // DisplayMessage(newMessage3);
        }

        private void Update()
        {
            if (dialogueQueue.Count > 0 && isDisplayingDialogue is false)
            {
                string text = dialogueQueue.Peek().Text;
                float displayLength = text.Length * 0.1f;
                FactionType senderType = dialogueQueue.Peek().Sender.Faction;

                if (senderType is FactionType.Player)
                {
                    StartCoroutine(FadeTextEffect(dialogueQueue.Dequeue().Text, displayLength, Color.green));
                }
                else if (senderType is FactionType.Scavenger)
                {
                    StartCoroutine(FadeTextEffect(dialogueQueue.Dequeue().Text, displayLength, Color.red));
                }
                else
                {
                    StartCoroutine(FadeTextEffect(dialogueQueue.Dequeue().Text, displayLength));
                }
            }
        }

        private void DisplayMessage(Message message)
        {
            dialogueQueue.Enqueue(message);
        }


        public IEnumerator FadeTextEffect(string message, float displayLength, Color color = default)
        {
            dialogueText.text = message;
            isDisplayingDialogue = true;

            dialogueText.color = color;

            dialogueText.CrossFadeAlpha(0, 0, false);
            dialogueText.CrossFadeAlpha(1, 1.15f, false);
            yield return new WaitForSeconds(displayLength);
            dialogueText.CrossFadeAlpha(0, 1.15f, false);
            yield return new WaitForSeconds(2);

            isDisplayingDialogue = false;
        }
    }
}