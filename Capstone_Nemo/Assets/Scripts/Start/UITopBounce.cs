using UnityEngine;

public class UITopBreathing : MonoBehaviour
{
    [Header("Breathing Settings")]
    [SerializeField] private float breathAmount = 0.025f; // 움직이는 정도
    [SerializeField] private float breathSpeed = 1.2f;    // 속도

    private RectTransform rectTransform;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    private void Update()
    {
        // 0 ~ 1 사이로 부드럽게 반복
        float t = (Mathf.Sin(Time.time * breathSpeed) + 1f) * 0.5f;

        // 너무 기계적으로 보이지 않게 부드러운 곡선 적용
        t = Mathf.SmoothStep(0f, 1f, t);

        float yScale = 1f + (t * breathAmount);

        rectTransform.localScale = new Vector3(
            originalScale.x,
            originalScale.y * yScale,
            originalScale.z
        );
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
    }
}
