using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookPageFlipAnimator : MonoBehaviour
{
    public enum FlipDirection
    {
        Next,
        Prev
    }

    [Header("기준 책 패널")]
    [SerializeField] private RectTransform dogamPanel;

    [Header("넘김용 페이지")]
    [SerializeField] private RectTransform flipPage;
    [SerializeField] private Image flipPageImage;

    [Header("그림자")]
    [SerializeField] private CanvasGroup flipShadow;

    [Header("입력 잠금")]
    [SerializeField] private CanvasGroup inputBlocker;

    [Header("애니메이션")]
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("페이지 영역 비율")]
    [Tooltip("책 전체에서 오른쪽 페이지가 시작되는 X 비율 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float rightPageStartXNormalized = 0.5f;

    [Tooltip("넘김 페이지의 위/아래 여백")]
    [SerializeField] private float verticalPadding = 0f;

    [Tooltip("넘김 페이지의 좌/우 여백")]
    [SerializeField] private float horizontalPadding = 0f;

    public bool IsFlipping { get; private set; }

    public void PlayFlip(FlipDirection direction, Sprite pageSprite, Action onMidFlipSwap, Action onComplete = null)
    {
        if (IsFlipping) return;
        StartCoroutine(CoFlip(direction, pageSprite, onMidFlipSwap, onComplete));
    }

    private IEnumerator CoFlip(FlipDirection direction, Sprite pageSprite, Action onMidFlipSwap, Action onComplete)
    {
        IsFlipping = true;
        SetInputBlocked(true);

        if (dogamPanel == null || flipPage == null || flipPageImage == null)
        {
            onMidFlipSwap?.Invoke();
            onComplete?.Invoke();
            SetInputBlocked(false);
            IsFlipping = false;
            yield break;
        }

        SetupFlipPageRect(direction);

        flipPage.localScale = Vector3.one;
        flipPage.localRotation = Quaternion.identity;
        flipPage.pivot = new Vector2(0.5f, 0.5f);

        flipPageImage.sprite = pageSprite;
        //flipPageImage.SetNativeSize();

        flipPage.gameObject.SetActive(true);
        flipPage.SetAsLastSibling();

        if (flipShadow != null)
        {
            flipShadow.alpha = 0f;
            flipShadow.gameObject.SetActive(true);
        }

        Vector3 startEuler;
        Vector3 endEuler;

        if (direction == FlipDirection.Next)
        {
            // 오른쪽 페이지 -> 왼쪽으로
            SetPivotKeepingPosition(flipPage, new Vector2(0f, 0.5f));
            startEuler = Vector3.zero;
            endEuler = new Vector3(0f, -180f, 0f);
        }
        else
        {
            // 왼쪽 페이지 -> 오른쪽으로
            SetPivotKeepingPosition(flipPage, new Vector2(1f, 0.5f));
            startEuler = Vector3.zero;
            endEuler = new Vector3(0f, 180f, 0f);
        }

        flipPage.localEulerAngles = startEuler;

        bool swapped = false;
        float time = 0f;
        float half = duration * 0.5f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float eased = flipCurve.Evaluate(t);

            flipPage.localEulerAngles = Vector3.LerpUnclamped(startEuler, endEuler, eased);

            if (flipShadow != null)
            {
                float shadowT = Mathf.Sin(t * Mathf.PI);
                flipShadow.alpha = shadowT * 0.45f;
            }

            if (!swapped && time >= half)
            {
                swapped = true;
                onMidFlipSwap?.Invoke();
            }

            yield return null;
        }

        if (!swapped)
            onMidFlipSwap?.Invoke();

        flipPage.localEulerAngles = endEuler;

        if (flipShadow != null)
        {
            flipShadow.alpha = 0f;
            flipShadow.gameObject.SetActive(false);
        }

        flipPage.gameObject.SetActive(false);

        onComplete?.Invoke();

        SetInputBlocked(false);
        IsFlipping = false;
    }

    private void SetupFlipPageRect(FlipDirection direction)
    {
        Rect rect = dogamPanel.rect;

        float panelWidth = rect.width;
        float panelHeight = rect.height;

        float pageWidth = panelWidth * 0.5f - horizontalPadding * 2f;
        float pageHeight = panelHeight - verticalPadding * 2f;

        flipPage.SetParent(dogamPanel.parent, false);

        flipPage.anchorMin = new Vector2(0.5f, 0.5f);
        flipPage.anchorMax = new Vector2(0.5f, 0.5f);
        flipPage.pivot = new Vector2(0.5f, 0.5f);
        flipPage.sizeDelta = new Vector2(pageWidth, pageHeight);

        float leftPageCenterX = dogamPanel.anchoredPosition.x - (panelWidth * 0.25f);
        float rightPageCenterX = dogamPanel.anchoredPosition.x + (panelWidth * 0.25f);
        float centerY = dogamPanel.anchoredPosition.y;

        if (direction == FlipDirection.Next)
        {
            flipPage.anchoredPosition = new Vector2(rightPageCenterX, centerY);
        }
        else
        {
            flipPage.anchoredPosition = new Vector2(leftPageCenterX, centerY);
        }

        flipPage.localScale = Vector3.one;
        flipPage.localRotation = Quaternion.identity;
    }

    private void SetInputBlocked(bool blocked)
    {
        if (inputBlocker == null) return;

        inputBlocker.blocksRaycasts = blocked;
        inputBlocker.interactable = !blocked;
        inputBlocker.alpha = 0f;
    }

    private void SetPivotKeepingPosition(RectTransform rt, Vector2 newPivot)
    {
        Vector2 size = rt.rect.size;
        Vector2 deltaPivot = newPivot - rt.pivot;
        Vector2 deltaPosition = new Vector2(deltaPivot.x * size.x, deltaPivot.y * size.y);

        rt.pivot = newPivot;
        rt.anchoredPosition += deltaPosition;
    }
}
