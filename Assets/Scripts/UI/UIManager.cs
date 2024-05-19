using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Messages")]
    public static UIManager Instance;
    public TextMeshProUGUI minorAlert;

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

    public void DisplayMinorAlert(string alertText)
    {
        minorAlert.text = alertText;
        StartCoroutine(FadeTextEffect(minorAlert));
    }

    public IEnumerator FadeTextEffect(TextMeshProUGUI text)
    {
        text.CrossFadeAlpha(0, 0, false);
        text.CrossFadeAlpha(1, 2.5f, false);
        yield return new WaitForSeconds(3);
        text.CrossFadeAlpha(0, 2f, false);
    }
}