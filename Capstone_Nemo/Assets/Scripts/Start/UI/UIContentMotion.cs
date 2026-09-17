using System;
using System.Collections;
using UnityEngine;

public class UIContentMotion : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Optional. If empty, this object's RectTransform is used.")]
    [SerializeField] private RectTransform target;

    [Header("Motion")]
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float moveDistance = 8f;
    [SerializeField] private float minimumScale = 0.98f;

    [Header("Direction")]
    [Tooltip("When enabled, Next and Prev use opposite vertical directions.")]
    [SerializeField] private bool useDirection = true;

    private Coroutine motionCoroutine;

    public bool IsPlaying => motionCoroutine != null;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;
    }

    public void PlaySwap(Action middleAction, bool forward)
    {
        if (middleAction == null)
            return;

        if (!EnsureTarget())
        {
            middleAction.Invoke();
            Canvas.ForceUpdateCanvases();
            return;
        }

        if (motionCoroutine != null)
            return;

        motionCoroutine =
            StartCoroutine(
                SwapRoutine(
                    middleAction,
                    forward
                )
            );
    }

    public void PlayOut(Action onComplete, bool forward)
    {
        if (!EnsureTarget())
        {
            onComplete?.Invoke();
            return;
        }

        if (motionCoroutine != null)
            return;

        motionCoroutine =
            StartCoroutine(
                OutRoutine(
                    onComplete,
                    forward
                )
            );
    }

    public void PlayIn(bool forward)
    {
        if (!EnsureTarget())
            return;

        if (motionCoroutine != null)
            StopCoroutine(motionCoroutine);

        motionCoroutine =
            StartCoroutine(
                InRoutine(forward)
            );
    }

    public void ResetImmediate()
    {
        if (!EnsureTarget())
            return;

        if (motionCoroutine != null)
        {
            StopCoroutine(motionCoroutine);
            motionCoroutine = null;
        }

        target.anchoredPosition = Vector2.zero;
        target.localScale = Vector3.one;
    }

    private IEnumerator SwapRoutine(
        Action middleAction,
        bool forward)
    {
        Vector2 basePosition = target.anchoredPosition;
        Vector3 baseScale = target.localScale;

        float direction =
            useDirection
                ? (forward ? 1f : -1f)
                : 1f;

        Vector2 outPosition =
            basePosition +
            new Vector2(
                0f,
                -moveDistance * direction
            );

        Vector2 inPosition =
            basePosition +
            new Vector2(
                0f,
                moveDistance * direction
            );

        Vector3 smallScale =
            GetSmallScale(baseScale);

        float halfDuration =
            Mathf.Max(
                0.01f,
                duration * 0.5f
            );

        yield return Animate(
            basePosition,
            outPosition,
            baseScale,
            smallScale,
            halfDuration,
            true
        );

        target.anchoredPosition = outPosition;
        target.localScale = smallScale;

        middleAction.Invoke();

        Canvas.ForceUpdateCanvases();

        // Wait one frame so layout, SetActive and lock-state changes settle.
        yield return null;

        target.anchoredPosition = inPosition;
        target.localScale = smallScale;

        yield return Animate(
            inPosition,
            basePosition,
            smallScale,
            baseScale,
            halfDuration,
            false
        );

        target.anchoredPosition = basePosition;
        target.localScale = baseScale;

        motionCoroutine = null;
    }

    private IEnumerator OutRoutine(
        Action onComplete,
        bool forward)
    {
        Vector2 basePosition = target.anchoredPosition;
        Vector3 baseScale = target.localScale;

        float direction =
            useDirection
                ? (forward ? 1f : -1f)
                : 1f;

        Vector2 outPosition =
            basePosition +
            new Vector2(
                0f,
                -moveDistance * direction
            );

        Vector3 smallScale =
            GetSmallScale(baseScale);

        yield return Animate(
            basePosition,
            outPosition,
            baseScale,
            smallScale,
            Mathf.Max(0.01f, duration * 0.5f),
            true
        );

        // Restore before the owning page is potentially disabled.
        target.anchoredPosition = basePosition;
        target.localScale = baseScale;

        motionCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator InRoutine(bool forward)
    {
        Vector2 basePosition = target.anchoredPosition;
        Vector3 baseScale = target.localScale;

        float direction =
            useDirection
                ? (forward ? 1f : -1f)
                : 1f;

        Vector2 inPosition =
            basePosition +
            new Vector2(
                0f,
                moveDistance * direction
            );

        Vector3 smallScale =
            GetSmallScale(baseScale);

        target.anchoredPosition = inPosition;
        target.localScale = smallScale;

        yield return Animate(
            inPosition,
            basePosition,
            smallScale,
            baseScale,
            Mathf.Max(0.01f, duration * 0.5f),
            false
        );

        target.anchoredPosition = basePosition;
        target.localScale = baseScale;

        motionCoroutine = null;
    }

    private IEnumerator Animate(
        Vector2 fromPosition,
        Vector2 toPosition,
        Vector3 fromScale,
        Vector3 toScale,
        float animationDuration,
        bool easeIn)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / animationDuration
                );

            float eased =
                easeIn
                    ? EaseInCubic(t)
                    : EaseOutCubic(t);

            target.anchoredPosition =
                Vector2.Lerp(
                    fromPosition,
                    toPosition,
                    eased
                );

            target.localScale =
                Vector3.Lerp(
                    fromScale,
                    toScale,
                    eased
                );

            yield return null;
        }
    }

    private Vector3 GetSmallScale(Vector3 baseScale)
    {
        return new Vector3(
            baseScale.x * minimumScale,
            baseScale.y * minimumScale,
            baseScale.z
        );
    }

    private bool EnsureTarget()
    {
        if (target == null)
            target = transform as RectTransform;

        return target != null;
    }

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
