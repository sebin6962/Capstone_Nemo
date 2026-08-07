using System.Collections;
using UnityEngine;

public class IntroLetterboxAnimator : MonoBehaviour
{
    [Header("검은 패널")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;

    [Header("연출 설정")]
    [SerializeField] private float showDuration = 0.7f;
    [SerializeField] private float hideDuration = 0.55f;

    [Tooltip("화면 양옆에 틈이 생기지 않도록 패널을 좌우로 확장하는 값")]
    [SerializeField] private float horizontalOverscan = 3f;

    [Tooltip("화면 중앙 쪽에도 미세한 틈이 보인다면 1~2 정도 설정")]
    [SerializeField] private float verticalOverscan = 1f;

    private Coroutine animationRoutine;

    private float topHiddenY;
    private float bottomHiddenY;

    private void Awake()
    {
        ConfigureBar(topBar, true);
        ConfigureBar(bottomBar, false);

        // CanvasScaler와 레이아웃 계산을 즉시 완료
        Canvas.ForceUpdateCanvases();

        CalculateHiddenPositions();
        SetHiddenImmediate();
    }

    private void ConfigureBar(RectTransform bar, bool isTop)
    {
        if (bar == null)
            return;

        // 가로 방향 완전 Stretch
        bar.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
        bar.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
        bar.pivot = new Vector2(0.5f, isTop ? 1f : 0f);

        // 화면보다 좌우로 조금 더 크게 만들어 빈틈 방지
        Vector2 offsetMin = bar.offsetMin;
        Vector2 offsetMax = bar.offsetMax;

        offsetMin.x = -horizontalOverscan;
        offsetMax.x = horizontalOverscan;

        bar.offsetMin = offsetMin;
        bar.offsetMax = offsetMax;
    }

    private void CalculateHiddenPositions()
    {
        float topHeight = topBar != null
            ? topBar.rect.height
            : 0f;

        float bottomHeight = bottomBar != null
            ? bottomBar.rect.height
            : 0f;

        // 패널 전체가 확실히 화면 밖으로 나가도록 약간 여유 추가
        topHiddenY = topHeight + verticalOverscan + 2f;
        bottomHiddenY = -(bottomHeight + verticalOverscan + 2f);
    }

    public void Show()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        /*
         * 여기서 gameObject.SetActive(true)를 호출하지 않는다.
         * 처음부터 오브젝트를 활성화해 두고 화면 밖에 숨겨야
         * 등장 순간 Canvas 리빌드로 인한 끊김이 발생하지 않는다.
         */
        animationRoutine = StartCoroutine(AnimateBars(true));
    }

    public void Hide()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateBars(false));
    }

    public void SetHiddenImmediate()
    {
        SetBarY(topBar, topHiddenY);
        SetBarY(bottomBar, bottomHiddenY);
    }

    public void SetVisibleImmediate()
    {
        // 화면 바깥 방향으로 조금 더 밀어서 외곽 틈 방지
        SetBarY(topBar, verticalOverscan);
        SetBarY(bottomBar, -verticalOverscan);
    }

    private IEnumerator AnimateBars(bool show)
    {
        float duration = show ? showDuration : hideDuration;

        float topStart = GetBarY(topBar);
        float bottomStart = GetBarY(bottomBar);

        // 화면 안쪽으로 1px 정도 겹치게 해 가장자리 틈 방지
        float topTarget = show
    ? verticalOverscan
    : topHiddenY;

        float bottomTarget = show
            ? -verticalOverscan
            : bottomHiddenY;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = duration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / duration);

            /*
             * Ease Out Cubic
             * 시작하자마자 확실히 움직이고 끝에서 부드럽게 감속한다.
             * 기존 SmoothStep의 '잠깐 멈췄다가 출발하는 느낌'을 줄여준다.
             */
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            SetBarY(
                topBar,
                Mathf.LerpUnclamped(topStart, topTarget, easedT)
            );

            SetBarY(
                bottomBar,
                Mathf.LerpUnclamped(bottomStart, bottomTarget, easedT)
            );

            yield return null;
        }

        SetBarY(topBar, topTarget);
        SetBarY(bottomBar, bottomTarget);

        animationRoutine = null;

        /*
         * Hide가 끝나도 gameObject.SetActive(false)를 하지 않는다.
         * 화면 밖에 있으므로 보이지 않으며 다음 Show에서도 끊기지 않는다.
         */
    }

    private float GetBarY(RectTransform bar)
    {
        return bar != null ? bar.anchoredPosition.y : 0f;
    }

    private void SetBarY(RectTransform bar, float y)
    {
        if (bar == null)
            return;

        Vector2 position = bar.anchoredPosition;
        position.y = y;
        bar.anchoredPosition = position;
    }
}