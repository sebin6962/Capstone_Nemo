using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BasketPanelPrefab : MonoBehaviour
{
    public TMP_Text itemNameText;
    public TMP_Text quantityText;
    public TMP_Text totalPriceText;

    private ShopData item;

    public void SetItem(ShopData itemData)
    {
        item = itemData;
    }

    public void UpdateDisplay(int quantity)
    {
        if (item == null || quantity <= 0) return;

        quantityText.text = $"{quantity}";
        itemNameText.text = item.itemName;
        totalPriceText.text = $"{item.price * quantity}º°ºû";
    }
}
