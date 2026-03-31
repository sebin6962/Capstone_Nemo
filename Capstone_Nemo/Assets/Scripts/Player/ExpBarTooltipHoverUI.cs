using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ExpBarTooltipHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ÂüÁ¶")]
    public PlayerLevelUI playerLevelUI;

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

        if (isHovering && playerLevelUI != null && tooltipText != null)
        {
            tooltipText.text = playerLevelUI.GetExpTooltipText();
        }

        float targetAlpha = isHovering ? 1f : 0f;
        tooltipGroup.alpha = Mathf.MoveTowards(
            tooltipGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playerLevelUI == null || tooltipPanel == null || tooltipText == null || tooltipGroup == null)
            return;

        tooltipText.text = playerLevelUI.GetExpTooltipText();
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
