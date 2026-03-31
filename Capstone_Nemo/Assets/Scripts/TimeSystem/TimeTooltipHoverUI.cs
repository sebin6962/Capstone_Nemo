using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TimeTooltipHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ÅøÆÁ UI")]
    public RectTransform tooltipPanel;
    public TMP_Text tooltipText;
    public CanvasGroup tooltipGroup;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 12f;

    private bool isHovering = false;

    private void Awake()
    {
        if (tooltipPanel != null && tooltipGroup == null)
            tooltipGroup = tooltipPanel.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        HideImmediate();
    }

    private void Update()
    {
        if (tooltipGroup == null) return;

        float targetAlpha = isHovering ? 1f : 0f;
        tooltipGroup.alpha = Mathf.MoveTowards(
            tooltipGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel == null || tooltipText == null || tooltipGroup == null) return;
        if (TimeManager.Instance == null) return;

        tooltipText.text = TimeManager.Instance.GetCurrentTimeTooltipText();
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    private void HideImmediate()
    {
        if (tooltipGroup == null) return;

        tooltipGroup.alpha = 0f;
        tooltipGroup.interactable = false;
        tooltipGroup.blocksRaycasts = false;
    }
}