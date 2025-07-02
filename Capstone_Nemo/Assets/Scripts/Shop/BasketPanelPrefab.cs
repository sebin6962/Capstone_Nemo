using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BasketPanelPrefab : MonoBehaviour
{
    public TMP_Text itemNameText;
    public TMP_Text quantityText;
    public TMP_Text totalPriceText;
    public Image itemImage;

    private ShopData item;

    public void SetItem(ShopData itemData)
    {
        item = itemData;
    }

    public void UpdateDisplay(int quantity)
    {
        if (item == null || quantity <= 0) return;

        quantityText.text = $"{quantity}";
        string displayName;
        if (!ItemTooltipDB.TooltipTexts.TryGetValue(item.itemName, out displayName))
            displayName = item.itemName;
        itemNameText.text = displayName;
        totalPriceText.text = $"{item.price * quantity}별빛";

        Sprite sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + item.itemName);
        if (sprite != null)
        {
            itemImage.sprite = sprite;
            itemImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[ShopItemSlot] 스프라이트 로드 실패: {item.itemName}");
            itemImage.enabled = false;
        }
    }
}
