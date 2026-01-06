using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardPopupPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI starText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rect;
    private Coroutine moveCo;
    private Coroutine fadeCo;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetContent(int exp, int star, string dagwaKeyOrName)
    {
        if (expText) expText.text = $"+{exp}";
        if (starText) starText.text = $"+{star}";
        if (messageText) messageText.text = $"{dagwaKeyOrName}을(를) 판매하였습니다.";
    }

    public void SetImmediatePos(Vector2 anchoredPos)
    {
        rect.anchoredPosition = anchoredPos;
    }

    public void PlayIn(Vector2 targetPos, float duration)
    {
        // 등장: 살짝 아래/투명에서 올라오며 1로
        if (moveCo != null) StopCoroutine(moveCo);
        if (fadeCo != null) StopCoroutine(fadeCo);

        canvasGroup.alpha = 0f;
        Vector2 start = targetPos + Vector2.down * 20f;
        rect.anchoredPosition = start;

        moveCo = StartCoroutine(MoveRoutine(start, targetPos, duration));
        fadeCo = StartCoroutine(FadeRoutine(0f, 1f, duration));
    }

    public void MoveTo(Vector2 targetPos, float duration)
    {
        if (moveCo != null) StopCoroutine(moveCo);
        moveCo = StartCoroutine(MoveRoutine(rect.anchoredPosition, targetPos, duration));
    }

    public void FadeOutAndDestroy(float duration)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeOutDestroyRoutine(duration));
    }

    private IEnumerator MoveRoutine(Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            rect.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, a));
            yield return null;
        }
        rect.anchoredPosition = to;
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, a);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator FadeOutDestroyRoutine(float duration)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(start, 0f, a);
            yield return null;
        }
        Destroy(gameObject);
    }
}
