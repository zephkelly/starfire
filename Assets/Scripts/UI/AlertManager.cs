using System.Collections;
using UnityEngine;
using TMPro;

namespace Starfire
{
    public class AlertManager : MonoBehaviour, IUIComponent
    {
        [Header("Minor Alert")]
        public TextMeshProUGUI minorAlert;
        private Queue minorAlertQueue = new Queue();
        private string lastMinorAlert;
        private bool isMinorAlertActive = false;

        private void Awake()
        {
            if (minorAlert == null)
            {
                Debug.LogError("Minor alert text not set in AlertManager");
            }
        }

        public void Show()
        {
            // throw new System.NotImplementedException();
        }

        public void Hide()
        {
            // throw new System.NotImplementedException();
        }
        
        public void DisplayMinorAlert(string alertText)
        {
            if (minorAlertQueue.Count > 0)
            {
                minorAlertQueue.Dequeue();
            }

            minorAlertQueue.Enqueue(alertText);
        }

        private void Update()
        {
            ShouldDisplayMinorAlert();
        }

        private void ShouldDisplayMinorAlert()
        {
            if (minorAlertQueue.Count > 0 && !isMinorAlertActive)
            {
                if (lastMinorAlert == minorAlertQueue.Peek().ToString())
                {
                    minorAlertQueue.Dequeue();
                    return;
                }

                lastMinorAlert = minorAlertQueue.Peek().ToString();
                StartCoroutine(FadeTextEffect(minorAlert, minorAlertQueue.Dequeue().ToString()));
            }
        }

        private IEnumerator FadeTextEffect(TextMeshProUGUI text, string message)
        {
            text.text = message;
            isMinorAlertActive = true;

            text.CrossFadeAlpha(0, 0, false);
            text.CrossFadeAlpha(1, 1.15f, false);
            yield return new WaitForSeconds(2.25f);
            text.CrossFadeAlpha(0, 1.15f, false);
            yield return new WaitForSeconds(2);

            isMinorAlertActive = false;
        }
    }
}