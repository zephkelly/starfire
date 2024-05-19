using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Messages")]
    public static UIManager Instance;
    public TextMeshProUGUI minorAlert;
    private bool isMinorAlertActive = false;

    private Queue minorAlertQueue = new Queue();
    private string lastMinorAlert;

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

    public void DisplayMinorAlert(string alertText)
    {
        if (minorAlertQueue.Count > 0)
        {
            minorAlertQueue.Dequeue();
        }

        minorAlertQueue.Enqueue(alertText);
    }

    public IEnumerator FadeTextEffect(TextMeshProUGUI text, string message)
    {
        text.text = message;
        isMinorAlertActive = true;

        text.CrossFadeAlpha(0, 0, false);
        text.CrossFadeAlpha(1, 2f, false);
        yield return new WaitForSeconds(3);
        text.CrossFadeAlpha(0, 2f, false);
        yield return new WaitForSeconds(2);

        isMinorAlertActive = false;
    }
}