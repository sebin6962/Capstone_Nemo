using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    private ShopData item;
    private ShopManager manager;

    public void Setup(ShopData newItem, ShopManager shopManager)
    {
        item = newItem;
        manager = shopManager;

        itemNameText.text = item.itemName;
        priceText.text = item.price + " º°ºû";

        plusButton.onClick.AddListener(OnPlusButtonClicked);
        minusButton.onClick.AddListener(OnMinusButtonClicked);

        UpdateDisplay(0);
    }
    
    void OnPlusButtonClicked()
    {
        manager.AdjustItem(item, +1);
    }

    void OnMinusButtonClicked()
    {
        manager.AdjustItem(item, -1);
    }

    public void UpdateDisplay(int quantity)
    {
        quantityText.text = $"{quantity} °³";
    }
}
