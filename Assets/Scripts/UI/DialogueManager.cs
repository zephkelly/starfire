using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Starfire
{
    public class DialogueManager : MonoBehaviour, IUIComponent
    {
        [SerializeField] private TextMeshProUGUI dialogueText;
        Queue<Message> dialogueQueue = new Queue<Message>();
        private bool isDisplayingDialogue = false;

        private void Awake()
        {
            if (dialogueText == null)
            {
                Debug.LogError("Dialogue text not set in DialogueManager");
            }
        }

        public void Show()
        {
            throw new System.NotImplementedException();
        }

        public void Hide()
        {
            throw new System.NotImplementedException();
        }

        public void DisplayMessage(Message message)
        {
            dialogueQueue.Enqueue(message);
        }

        private void Start()
        {
            var newSender = new Transponder("Scavenger", Faction.Enemy, 90000);
            var newReceiver = new Transponder("Player", Faction.Player, 90000);

            var newMessage = new Message(newSender, newReceiver, "Hello new traveller. First time in this sector? I'm here to help ya out!");
            var newMessage2 = new Message(newSender, newReceiver, "Just turn off your ships engines and we'll take care of the rest.");
            DisplayMessage(newMessage);
            DisplayMessage(newMessage2);
        }

        private void Update()
        {
            if (dialogueQueue.Count > 0 && isDisplayingDialogue is false)
            {
                string text = dialogueQueue.Peek().Text;
                float displayLength = text.Length * 0.1f;
                Faction senderType = dialogueQueue.Peek().Sender.Faction;

                if (senderType is Faction.Player)
                {
                    StartCoroutine(FadeTextEffect(dialogueQueue.Dequeue().Text, displayLength, Color.green));
                }
                else if (senderType is Faction.Enemy)
                {
                    StartCoroutine(FadeTextEffect(dialogueQueue.Dequeue().Text, displayLength, Color.red));
                }
                else
                {
                    StartCoroutine(FadeTextEffect(dialogueQueue.Dequeue().Text, displayLength));
                }
            }
        }

        private IEnumerator FadeTextEffect(string message, float displayLength, Color color = default)
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