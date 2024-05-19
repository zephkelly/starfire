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
        if (minorAlertQueue.Count > 0 && !isMinorAlertActive)
        {
            StartCoroutine(FadeTextEffect(minorAlert, minorAlertQueue.Dequeue().ToString()));
        }
    }

    public void DisplayMinorAlert(string alertText)
    {
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