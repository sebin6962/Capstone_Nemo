
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class EKeyGuideFloat : MonoBehaviour
{
    [Header("Floating Settings")]
    [SerializeField] private float moveDistance = 6f;
    [SerializeField] private float moveSpeed = 2f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private float elapsedTime;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        originalPosition = rectTransform.anchoredPosition;
        elapsedTime = 0f;
    }

    private void Update()
    {
        elapsedTime += Time.unscaledDeltaTime;

        float offsetY = Mathf.Sin(elapsedTime * moveSpeed)
                        * moveDistance;

        rectTransform.anchoredPosition =
            originalPosition + Vector2.up * offsetY;
    }

    private void OnDisable()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = originalPosition;
    }
}

