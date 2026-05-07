using System.Collections;
using TMPro;
using UnityEngine;

public class BubblePopupEffect : MonoBehaviour
{
    public enum ShowStyle
    {
        IntroSpread,
        NoticeBoing
    }

    [Header("References")]
    [SerializeField] private TMP_Text bubbleText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Intro Spread")]
    [SerializeField] private float introShowDuration = 0.28f;
    [SerializeField] private float introHideDuration = 0.18f;

    [Header("Notice Boing")]
    [SerializeField] private float noticeShowDuration = 0.24f;
    [SerializeField] private float noticeHideDuration = 0.18f;

    [Header("Scale Presets")]
    [SerializeField] private float hiddenScaleMultiplier = 0.05f;
    [SerializeField] private float boingOvershootMultiplier = 1.18f;
    [SerializeField] private float boingSettleMultiplier = 0.96f;

    private Vector3 originalScale;
    private Vector3 hiddenScale;
    private Vector3 normalScale;
    private Vector3 boingOvershootScale;
    private Vector3 boingSettleScale;

    private Coroutine playingCoroutine;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        gameObject.SetActive(true);

        // 씬에 배치해둔 원래 크기를 기준으로 저장
        originalScale = cachedTransform.localScale;
        normalScale = originalScale;
        hiddenScale = originalScale * hiddenScaleMultiplier;
        boingOvershootScale = originalScale * boingOvershootMultiplier;
        boingSettleScale = originalScale * boingSettleMultiplier;

        canvasGroup.alpha = 0f;
        cachedTransform.localScale = hiddenScale;
    }

    public void HideImmediate()
    {
        if (playingCoroutine != null)
        {
            StopCoroutine(playingCoroutine);
            playingCoroutine = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;
    }

    public Coroutine Play(string message, float visibleDuration, ShowStyle style)
    {
        Debug.Log($"[BubblePopupEffect] Play 호출됨 / message={message} / style={style}");

        if (bubbleText != null)
            bubbleText.text = message;
        else
            Debug.LogWarning("[BubblePopupEffect] bubbleText가 연결되지 않았습니다.");

        if (canvasGroup == null)
            Debug.LogWarning("[BubblePopupEffect] canvasGroup이 없습니다.");

        if (playingCoroutine != null)
            StopCoroutine(playingCoroutine);

        gameObject.SetActive(true);

        // 시작 상태 강제 초기화
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;

        playingCoroutine = StartCoroutine(PlayRoutine(visibleDuration, style));
        return playingCoroutine;
    }

    private IEnumerator PlayRoutine(float visibleDuration, ShowStyle style)
    {
        Debug.Log("[BubblePopupEffect] PlayRoutine 시작");

        switch (style)
        {
            case ShowStyle.IntroSpread:
                yield return StartCoroutine(PlayIntroShow());
                break;

            case ShowStyle.NoticeBoing:
                yield return StartCoroutine(PlayNoticeShow());
                break;
        }

        yield return new WaitForSecondsRealtime(visibleDuration);

        switch (style)
        {
            case ShowStyle.IntroSpread:
                yield return StartCoroutine(PlayHide(introHideDuration));
                break;

            case ShowStyle.NoticeBoing:
                yield return StartCoroutine(PlayHide(noticeHideDuration));
                break;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;

        playingCoroutine = null;
        Debug.Log("[BubblePopupEffect] PlayRoutine 종료");
    }

    private IEnumerator PlayIntroShow()
    {
        float t = 0f;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;

        while (t < introShowDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / introShowDuration);
            float eased = EaseOutBackLite(p);

            if (cachedTransform != null)
                cachedTransform.localScale = Vector3.LerpUnclamped(hiddenScale, normalScale, eased);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, p);

            yield return null;
        }

        if (cachedTransform != null)
            cachedTransform.localScale = normalScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private IEnumerator PlayNoticeShow()
    {
        float first = noticeShowDuration * 0.55f;
        float second = noticeShowDuration * 0.25f;
        float third = noticeShowDuration * 0.20f;

        float t = 0f;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;

        while (t < first)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / first);

            if (cachedTransform != null)
                cachedTransform.localScale = Vector3.LerpUnclamped(hiddenScale, boingOvershootScale, EaseOutBackStrong(p));

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, p);

            yield return null;
        }

        t = 0f;
        while (t < second)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / second);

            if (cachedTransform != null)
                cachedTransform.localScale = Vector3.Lerp(boingOvershootScale, boingSettleScale, p);

            yield return null;
        }

        t = 0f;
        while (t < third)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / third);

            if (cachedTransform != null)
                cachedTransform.localScale = Vector3.Lerp(boingSettleScale, normalScale, p);

            yield return null;
        }

        if (cachedTransform != null)
            cachedTransform.localScale = normalScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private IEnumerator PlayHide(float duration)
    {
        float t = 0f;
        Vector3 startScale = cachedTransform != null ? cachedTransform.localScale : hiddenScale;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);

            if (cachedTransform != null)
                cachedTransform.localScale = Vector3.Lerp(startScale, hiddenScale, EaseInBack(p));

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, p);

            yield return null;
        }

        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private float EaseOutBackLite(float x)
    {
        const float c1 = 1.2f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseOutBackStrong(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseInBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * x * x * x - c1 * x * x;
    }
}
