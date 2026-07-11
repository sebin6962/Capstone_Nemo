using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIPanelDropBounce : MonoBehaviour
{
    [Header("애니메이션 대상")]
    [SerializeField] private RectTransform target;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("팝업 설정")]
    [Range(0.1f, 1f)]
    [SerializeField] private float startScale = 0.85f;

    [Min(0.01f)]
    [SerializeField] private float duration = 0.18f;

    [Tooltip("1보다 크면 마지막에 살짝 커졌다가 원래 크기로 돌아옵니다.")]
    [Range(1f, 1.2f)]
    [SerializeField] private float overshootScale = 1.04f;

    [Header("기타")]
    [SerializeField] private bool fadeIn = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 originalScale;
    private Coroutine animationCoroutine;
    private bool initialized;

    private void Reset()
    {
        target = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        Initialize();

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        ResetPose();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        if (target == null)
            target = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        originalScale = target.localScale;
        initialized = true;
    }

    public void Play()
    {
        Initialize();

        if (!isActiveAndEnabled)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        // 화면에 그려지기 전에 시작 상태를 즉시 적용
        target.localScale = originalScale * startScale;

        if (fadeIn && canvasGroup != null)
            canvasGroup.alpha = 0f;

        animationCoroutine = StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();

            float progress = Mathf.Clamp01(elapsed / duration);

            // 빠르게 커진 뒤 자연스럽게 감속
            float easedProgress = EaseOutCubic(progress);

            // 시작 크기 → 살짝 큰 크기
            float scale = Mathf.Lerp(
                startScale,
                overshootScale,
                easedProgress
            );

            target.localScale = originalScale * scale;

            if (fadeIn && canvasGroup != null)
                canvasGroup.alpha = easedProgress;

            yield return null;
        }

        // 살짝 커진 상태에서 원래 크기로 복귀
        float settleDuration = duration * 0.45f;
        elapsed = 0f;

        while (elapsed < settleDuration)
        {
            elapsed += GetDeltaTime();

            float progress = Mathf.Clamp01(
                elapsed / settleDuration
            );

            float easedProgress = EaseOutCubic(progress);

            float scale = Mathf.Lerp(
                overshootScale,
                1f,
                easedProgress
            );

            target.localScale = originalScale * scale;

            yield return null;
        }

        ResetPose();
        animationCoroutine = null;
    }

    private void ResetPose()
    {
        if (!initialized || target == null)
            return;

        target.localScale = originalScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private float EaseOutCubic(float value)
    {
        return 1f - Mathf.Pow(1f - value, 3f);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }
}
