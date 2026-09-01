using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private Image bubbleImage;

    [Header("Auto Size")]
    [Tooltip("텍스트 바깥 여백의 합입니다. X는 좌우 합, Y는 위아래 합입니다.")]
    [SerializeField] private Vector2 totalPadding = new Vector2(40f, 40f);

    [SerializeField, Min(1f)] private float minBubbleWidth = 140f;
    [SerializeField, Min(1f)] private float minBubbleHeight = 80f;

    [Tooltip("이 너비를 넘으면 텍스트가 다음 줄로 내려갑니다.")]
    [SerializeField, Min(1f)] private float maxBubbleWidth = 320f;

    [SerializeField] private bool roundToWholePixels = true;

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
    private RectTransform bubbleTextRect;

    private void Awake()
    {
        cachedTransform = transform;

        if (bubbleRect == null)
            bubbleRect = transform as RectTransform;

        if (bubbleImage == null)
            bubbleImage = GetComponent<Image>();

        if (bubbleText != null)
            bubbleTextRect = bubbleText.rectTransform;

        if (bubbleImage != null)
            bubbleImage.type = Image.Type.Sliced;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        gameObject.SetActive(true);

        // 씬에 배치해둔 원래 스케일을 기준으로 저장한다.
        originalScale = cachedTransform.localScale;
        normalScale = originalScale;
        hiddenScale = originalScale * hiddenScaleMultiplier;
        boingOvershootScale = originalScale * boingOvershootMultiplier;
        boingSettleScale = originalScale * boingSettleMultiplier;

        canvasGroup.alpha = 0f;
        cachedTransform.localScale = hiddenScale;
    }

    private void OnValidate()
    {
        minBubbleWidth = Mathf.Max(1f, minBubbleWidth);
        minBubbleHeight = Mathf.Max(1f, minBubbleHeight);
        maxBubbleWidth = Mathf.Max(minBubbleWidth, maxBubbleWidth);

        if (bubbleRect == null)
            bubbleRect = transform as RectTransform;

        if (bubbleImage == null)
            bubbleImage = GetComponent<Image>();

        if (bubbleImage != null)
            bubbleImage.type = Image.Type.Sliced;

        if (bubbleText != null)
            bubbleTextRect = bubbleText.rectTransform;
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

        if (bubbleText != null)
            bubbleText.maxVisibleCharacters = int.MaxValue;
    }

    public Coroutine Play(
        string message,
        float visibleDuration,
        ShowStyle style,
        bool useTyping = false,
        float typingInterval = 0.04f)
    {
        Debug.Log($"[BubblePopupEffect] Play 호출됨 / message={message} / style={style} / typing={useTyping}");

        if (bubbleText != null)
        {
            bubbleText.text = message;

            // 타이핑을 시작하기 전에 전체 문장 기준으로 말풍선 크기를 확정한다.
            ResizeBubbleToMessage(message);

            bubbleText.ForceMeshUpdate();
            bubbleText.maxVisibleCharacters = useTyping ? 0 : int.MaxValue;
        }
        else
        {
            Debug.LogWarning("[BubblePopupEffect] bubbleText가 연결되지 않았습니다.");
        }

        if (canvasGroup == null)
            Debug.LogWarning("[BubblePopupEffect] canvasGroup이 없습니다.");

        if (playingCoroutine != null)
            StopCoroutine(playingCoroutine);

        gameObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (cachedTransform != null)
            cachedTransform.localScale = hiddenScale;

        playingCoroutine = StartCoroutine(
            PlayRoutine(visibleDuration, style, useTyping, typingInterval)
        );

        return playingCoroutine;
    }

    private void ResizeBubbleToMessage(string message)
    {
        if (bubbleText == null)
            return;

        if (bubbleRect == null)
            bubbleRect = transform as RectTransform;

        if (bubbleTextRect == null)
            bubbleTextRect = bubbleText.rectTransform;

        if (bubbleRect == null || bubbleTextRect == null)
            return;

        if (bubbleImage != null && bubbleImage.type != Image.Type.Sliced)
            bubbleImage.type = Image.Type.Sliced;

        string safeMessage = message ?? string.Empty;

        bubbleText.enableWordWrapping = true;

        float horizontalPadding = Mathf.Max(0f, totalPadding.x);
        float verticalPadding = Mathf.Max(0f, totalPadding.y);

        float minTextWidth = Mathf.Max(1f, minBubbleWidth - horizontalPadding);
        float maxTextWidth = Mathf.Max(minTextWidth, maxBubbleWidth - horizontalPadding);

        // 줄바꿈하지 않았을 때의 문장 너비를 먼저 측정한다.
        Vector2 singleLinePreferred = bubbleText.GetPreferredValues(
            safeMessage,
            Mathf.Infinity,
            Mathf.Infinity
        );

        // 짧은 문장은 가로로 줄이고, 긴 문장은 최대 너비에서 줄바꿈한다.
        float textWidth = Mathf.Clamp(
            singleLinePreferred.x,
            minTextWidth,
            maxTextWidth
        );

        Vector2 wrappedPreferred = bubbleText.GetPreferredValues(
            safeMessage,
            textWidth,
            Mathf.Infinity
        );

        float textHeight = Mathf.Max(1f, wrappedPreferred.y);
        float bubbleWidth = Mathf.Max(minBubbleWidth, textWidth + horizontalPadding);
        float bubbleHeight = Mathf.Max(minBubbleHeight, textHeight + verticalPadding);

        if (roundToWholePixels)
        {
            textWidth = Mathf.Ceil(textWidth);
            textHeight = Mathf.Ceil(textHeight);
            bubbleWidth = Mathf.Ceil(bubbleWidth);
            bubbleHeight = Mathf.Ceil(bubbleHeight);
        }

        bubbleTextRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            textWidth
        );

        bubbleTextRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            textHeight
        );

        bubbleRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            bubbleWidth
        );

        bubbleRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            bubbleHeight
        );

        bubbleText.ForceMeshUpdate();
    }

    private IEnumerator PlayRoutine(
        float visibleDuration,
        ShowStyle style,
        bool useTyping,
        float typingInterval)
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

        if (useTyping)
            yield return StartCoroutine(PlayTyping(typingInterval));

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

    private IEnumerator PlayTyping(float typingInterval)
    {
        if (bubbleText == null)
            yield break;

        bubbleText.ForceMeshUpdate();

        int characterCount = bubbleText.textInfo.characterCount;
        float interval = Mathf.Max(0.01f, typingInterval);

        bubbleText.maxVisibleCharacters = 0;

        for (int i = 1; i <= characterCount; i++)
        {
            bubbleText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(interval);
        }

        bubbleText.maxVisibleCharacters = int.MaxValue;
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
        Vector3 startScale = cachedTransform != null
            ? cachedTransform.localScale
            : hiddenScale;

        float startAlpha = canvasGroup != null
            ? canvasGroup.alpha
            : 1f;

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
