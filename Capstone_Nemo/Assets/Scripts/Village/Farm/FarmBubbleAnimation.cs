using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tail-pivot pop animation based on AltarSpeechBubble, without locking the player.
/// Put this on the bubble prefab root. Assign the child RectTransform whose
/// pivot is (0.5, 0) to Bubble Visual.
/// </summary>
public class FarmBubbleAnimation : MonoBehaviour
{
    [Header("Bubble parts")]
    [SerializeField] private RectTransform bubbleVisual;
    [SerializeField] private Image itemImage;

    [Header("Animation (seconds)")]
    [SerializeField, Min(0f)] private float appearDuration = 0.25f;
    [SerializeField, Min(0f)] private float disappearDuration = 0.20f;

    private Vector3 fullScale;
    private Coroutine animationRoutine;
    private bool isShowing;
    private bool isClosing;

    private void Awake()
    {
        if (bubbleVisual == null)
        {
            Debug.LogError("[FarmBubble] Assign Bubble Visual.", this);
            enabled = false;
            return;
        }

        fullScale = bubbleVisual.localScale;
        bubbleVisual.localScale = Vector3.zero;
        // Decorative world UI must not intercept clicks intended for the field.
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    public void SetIcon(Sprite sprite)
    {
        if (itemImage == null)
            return;
        itemImage.sprite = sprite;
        itemImage.enabled = sprite != null;
    }

    public void Show()
    {
        if (!enabled || isClosing || isShowing)
            return;

        isShowing = true;
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(ScaleTo(fullScale, appearDuration));
    }

    public void HideAndDestroy()
    {
        if (isClosing)
            return;
        isClosing = true;
        isShowing = false;
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(HideRoutine());
    }

    public void HideImmediately()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        isClosing = true;
        isShowing = false;

        if (bubbleVisual != null)
            bubbleVisual.localScale = Vector3.zero;

        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private IEnumerator HideRoutine()
    {
        yield return ScaleTo(Vector3.zero, disappearDuration);
        Destroy(gameObject);
    }

    private IEnumerator ScaleTo(Vector3 target, float duration)
    {
        Vector3 start = bubbleVisual.localScale;
        if (duration <= 0f)
        {
            bubbleVisual.localScale = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            bubbleVisual.localScale = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }
        bubbleVisual.localScale = target;
        animationRoutine = null;
    }
}
