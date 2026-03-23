using UnityEngine;
using UnityEngine.EventSystems;

public class RecipeItemTooltipSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform tooltipAnchor;

    private string itemKey;
    private string tooltipText;

    public void SetTooltip(string key)
    {
        itemKey = key?.Trim();

        if (string.IsNullOrEmpty(itemKey))
        {
            tooltipText = "";
            return;
        }

        if (!ItemTooltipDB.TooltipTexts.TryGetValue(itemKey, out tooltipText) &&
            !ItemTooltipDB.TooltipTexts.TryGetValue(itemKey.ToLower(), out tooltipText))
        {
            tooltipText = itemKey;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(tooltipText) || InventoryTooltipManager.Instance == null)
            return;

        RectTransform target = tooltipAnchor != null
            ? tooltipAnchor
            : GetComponent<RectTransform>();

        InventoryTooltipManager.Instance.Show(tooltipText, target);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryTooltipManager.Instance == null) return;
        InventoryTooltipManager.Instance.Hide();
    }
}
