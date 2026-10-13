using System.Collections;
using UnityEngine;

// 가게 마감 알림과 같은 이동/흔들림 효과. 문구는 TMP에서 직접 설정.
public class TreeEndingNoticeUI : MonoBehaviour
{
    [Header("알림 패널")]
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private CanvasGroup noticeGroup;

    [Header("효과 설정")]
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float shakeDuration = 0.22f;
    [SerializeField] private float shakeStrength = 8f;
    [SerializeField] private float shakeFrequency = 32f;

    private RectTransform panelRect;
    private Vector2 shownPosition;
    private Coroutine routine;

    private void Awake()
    {
        if (noticePanel == null)
        {
            Debug.LogError("[TreeEndingNoticeUI] Notice Panel을 연결하세요.", this);
            return;
        }

        panelRect = noticePanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            Debug.LogError("[TreeEndingNoticeUI] 패널에 RectTransform이 필요합니다.", this);
            return;
        }
        if (noticeGroup == null)
            noticeGroup = noticePanel.GetComponent<CanvasGroup>();

        shownPosition = panelRect.anchoredPosition;
        if (noticeGroup != null) noticeGroup.alpha = 1f;
        noticePanel.SetActive(false);
    }

    public void ShowNotice()
    {
        if (panelRect == null || !isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        panelRect.anchoredPosition = shownPosition;
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        noticePanel.SetActive(true);
        if (noticeGroup != null) noticeGroup.alpha = 1f;

        float parentWidth = Screen.width;
        if (panelRect.parent is RectTransform parentRect)
            parentWidth = parentRect.rect.width;
        Vector2 hiddenPosition = shownPosition + Vector2.left *
            (parentWidth + panelRect.rect.width);

        yield return Slide(hiddenPosition, shownPosition);

        float time = 0f;
        while (time < shakeDuration)
        {
            time += Time.deltaTime;
            float damping = 1f - Mathf.Clamp01(time / Mathf.Max(shakeDuration, 0.0001f));
            float offset = Mathf.Sin(time * shakeFrequency) * shakeStrength * damping;
            panelRect.anchoredPosition = shownPosition + new Vector2(offset, 0f);
            yield return null;
        }
        panelRect.anchoredPosition = shownPosition;

        yield return new WaitForSeconds(displayDuration);
        yield return Slide(shownPosition, hiddenPosition);
        panelRect.anchoredPosition = shownPosition;
        noticePanel.SetActive(false);
        routine = null;
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float time = 0f;
        float duration = Mathf.Max(0.0001f, slideDuration);
        panelRect.anchoredPosition = from;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
            panelRect.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
        panelRect.anchoredPosition = to;
    }
}
