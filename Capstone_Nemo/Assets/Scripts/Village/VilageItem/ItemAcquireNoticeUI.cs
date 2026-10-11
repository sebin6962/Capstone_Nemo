
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemAcquireNoticeUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject panel;
    public CanvasGroup canvasGroup;
    public TMP_Text messageText;
    public Image itemIcon;

    [Header("연출 시간")]
    public float slideDuration = 0.5f;
    public float showDuration = 3f;

    [Header("흔들림")]
    public float shakeDuration = 0.22f;
    public float shakeStrength = 8f;
    public float shakeFrequency = 32f;

    private Coroutine currentCo;
    private RectTransform panelRect;
    private Vector2 shownPosition;
    private bool positionCached;

    private void Awake()
    {
        if (panel == null)
            panel = gameObject;

        if (canvasGroup == null)
            canvasGroup = panel.GetComponent<CanvasGroup>();

        panelRect = panel.GetComponent<RectTransform>();

        if (panelRect != null)
        {
            shownPosition = panelRect.anchoredPosition;
            positionCached = true;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);

        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// 아이템 이름과 실제 획득 개수 표시
    /// </summary>
    public void ShowAcquire(string itemName, Sprite sprite, int count)
    {
        ShowMessageInternal(
            $"{itemName} X{count}",
            sprite,
            sprite != null
        );
    }

    // 기존 호출부 호환용
    public void ShowAcquire(string itemName, Sprite sprite)
    {
        ShowAcquire(itemName, sprite, 1);
    }

    public void ShowAcquire(string itemName, int count)
    {
        ShowMessageInternal(
            $"{itemName} X{count}",
            null,
            false
        );
    }

    public void ShowAcquire(string itemName)
    {
        ShowAcquire(itemName, 1);
    }

    /// <summary>
    /// 일반 알림
    /// </summary>
    public void ShowMessage(string message)
    {
        ShowMessageInternal(message, null, false);
    }

    private void ShowMessageInternal(
        string message,
        Sprite sprite,
        bool showIcon)
    {
        if (currentCo != null)
        {
            StopCoroutine(currentCo);
            currentCo = null;
        }

        ResetPanelVisualState();
        currentCo = StartCoroutine(
            ShowRoutine(message, sprite, showIcon)
        );
    }

    private IEnumerator ShowRoutine(
        string message,
        Sprite sprite,
        bool showIcon)
    {
        if (panel == null || messageText == null)
            yield break;

        if (panelRect == null)
            panelRect = panel.GetComponent<RectTransform>();

        if (panelRect == null)
            yield break;

        if (!positionCached)
        {
            shownPosition = panelRect.anchoredPosition;
            positionCached = true;
        }

        messageText.text = message;

        if (itemIcon != null)
        {
            itemIcon.sprite = sprite;
            itemIcon.gameObject.SetActive(
                showIcon && sprite != null
            );
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        panel.SetActive(true);

        float parentWidth = Screen.width;

        if (panelRect.parent is RectTransform parentRect)
            parentWidth = parentRect.rect.width;

        Vector2 hiddenPosition =
            shownPosition +
            Vector2.left * (parentWidth + panelRect.rect.width);

        panelRect.anchoredPosition = hiddenPosition;

        // 슬라이드 인
        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.deltaTime;

            float progress = slideDuration <= 0f
                ? 1f
                : Mathf.Clamp01(t / slideDuration);

            progress = Mathf.SmoothStep(
                0f, 1f, progress
            );

            panelRect.anchoredPosition = Vector2.Lerp(
                hiddenPosition,
                shownPosition,
                progress
            );

            yield return null;
        }

        panelRect.anchoredPosition = shownPosition;

        // 좌우 흔들림
        yield return StartCoroutine(
            ShakeAlertPanel(panelRect)
        );

        // 유지
        yield return new WaitForSeconds(showDuration);

        // 슬라이드 아웃
        t = 0f;

        while (t < slideDuration)
        {
            t += Time.deltaTime;

            float progress = slideDuration <= 0f
                ? 1f
                : Mathf.Clamp01(t / slideDuration);

            progress = Mathf.SmoothStep(
                0f, 1f, progress
            );

            panelRect.anchoredPosition = Vector2.Lerp(
                shownPosition,
                hiddenPosition,
                progress
            );

            yield return null;
        }

        ResetPanelVisualState();
        panel.SetActive(false);
        currentCo = null;
    }

    private IEnumerator ShakeAlertPanel(RectTransform target)
    {
        if (target == null)
            yield break;

        Vector2 originalPos = target.anchoredPosition;
        float time = 0f;

        while (time < shakeDuration)
        {
            time += Time.deltaTime;

            float normalized = shakeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(time / shakeDuration);

            float damping = 1f - normalized;

            float offsetX =
                Mathf.Sin(time * shakeFrequency) *
                shakeStrength *
                damping;

            target.anchoredPosition =
                originalPos + new Vector2(offsetX, 0f);

            yield return null;
        }

        target.anchoredPosition = originalPos;
    }

    private void ResetPanelVisualState()
    {
        if (panelRect != null && positionCached)
            panelRect.anchoredPosition = shownPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }
    }
}
