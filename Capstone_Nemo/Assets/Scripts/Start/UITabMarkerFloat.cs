using UnityEngine;

public class UITabMarkerFloat : MonoBehaviour
{
    [Header("위아래 움직임")]
    public float floatHeight = 8f;
    public float floatSpeed = 3f;

    [Header("살짝 튀는 스케일")]
    public float scaleAmount = 0.04f;
    public float scaleSpeed = 4f;

    [Header("옵션")]
    public bool randomPhase = true;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private float phase;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalLocalScale = rectTransform.localScale;

        phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed + phase) * floatHeight;

        rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(0f, yOffset);

        float scaleOffset = Mathf.Sin(Time.time * scaleSpeed + phase) * scaleAmount;
        rectTransform.localScale = originalLocalScale * (1f + scaleOffset);
    }

    private void OnDisable()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = originalLocalScale;
    }
}
