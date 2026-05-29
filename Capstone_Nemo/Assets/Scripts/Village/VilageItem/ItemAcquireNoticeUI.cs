using System.Collections;
using TMPro;
using UnityEngine;

public class ItemAcquireNoticeUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject panel;
    public CanvasGroup canvasGroup;
    public TMP_Text messageText;

    [Header("문구")]
    public string acquireFormat = "{0}을(를) 획득했다";

    [Header("시간")]
    public float fadeDuration = 0.5f;
    public float showDuration = 1.3f;

    private Coroutine currentCo;

    private void Awake()
    {
        if (panel == null)
            panel = gameObject;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (panel != null)
            panel.SetActive(false);
    }

    public void ShowAcquire(string itemName)
    {
        ShowMessage(string.Format(acquireFormat, itemName));
    }

    public void ShowMessage(string message)
    {
        if (currentCo != null)
            StopCoroutine(currentCo);

        currentCo = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        if (panel == null || canvasGroup == null || messageText == null)
            yield break;

        messageText.text = message;
        panel.SetActive(true);

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(showDuration);

        t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        panel.SetActive(false);
        currentCo = null;
    }
}