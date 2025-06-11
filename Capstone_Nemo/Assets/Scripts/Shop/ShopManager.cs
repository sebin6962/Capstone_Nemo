using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public GameObject shopPanel;
    public TMP_Text itemNameText;
    public TMP_Text totalPriceText;
    public TMP_Text allTotalPriceText;
    public Button buyButton;
    public Button resetButton;
    public Button closeButton;
    public List<ShopData> ShopItems;
    public int playerStar = 100;

    public Transform itemListParent;
    public Transform basketListParent;
    public GameObject itemSlotPrefab;
    public GameObject basketSlotPrefab;

    private Dictionary<ShopData, ShopBasketData> basketDict = new();
    private Dictionary<ShopData, ShopItemSlot> slotDict = new();
    private Dictionary<ShopData, BasketPanelPrefab> basketPanelDict = new();


    void Start()
    {
        ShopItems = new List<ShopData>
        {
            new ShopData{itemName = "���⸧", price = 10},
            new ShopData{itemName = "��", price = 15},
            new ShopData{itemName = "����", price = 5}
        };

  
    }

    public void OpenShop()
    {

        Debug.Log("OpenShop �����");

        /*if (itemSlotPrefab == null) Debug.LogError("itemSlotPrefab�� null�Դϴ�.");
        if (itemListParent == null) Debug.LogError("itemListParent�� null�Դϴ�.");
        if (ShopItems == null || ShopItems.Count == 0) Debug.LogError("ShopItems�� null�̰ų� ����ֽ��ϴ�.");*/

        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        slotDict.Clear();
        basketDict.Clear();

        foreach (var item in ShopItems)
        {
            var slotObj = Instantiate(itemSlotPrefab, itemListParent);
            var slot = slotObj.GetComponent<ShopItemSlot>();
            slot.Setup(item, this);

            slotDict[item] = slot;
            basketDict[item] = new ShopBasketData { item = item, quantity = 0 };
        }

        UpdateTotalPrice();
    }

    public void AdjustItem(ShopData item, int d)
    {
        var entry = basketDict[item];
        entry.quantity = Mathf.Max(0, entry.quantity + d);

        slotDict[item].UpdateDisplay(entry.quantity);
        UpdateTotalPrice();

        UpdateBasketPanel(item, entry.quantity);
    }

    void UpdateBasketPanel(ShopData item, int quantity)
    {
        if (quantity == 0)
        {
            if (basketPanelDict.ContainsKey(item))
            {
                Destroy(basketPanelDict[item].gameObject);
                basketPanelDict.Remove(item);
            }
        }
        else
        {
            if (basketPanelDict.ContainsKey(item))
            {
                basketPanelDict[item].UpdateDisplay(quantity);
            }
            else
            {
                var slotObj = Instantiate(basketSlotPrefab, basketListParent);
                var slot = slotObj.GetComponent<BasketPanelPrefab>();
                slot.SetItem(item);
                slot.UpdateDisplay(quantity);
                basketPanelDict[item] = slot;
            }
        }
    }

    void UpdateTotalPrice()
    {
        int total = 0;
        foreach (var e in basketDict.Values)
        {
            total += e.TotalPrice;
        }
        totalPriceText.text = $"{total} ����";
        allTotalPriceText.text = $"�� {total} ����";
        buyButton.interactable = (total > 0 && playerStar >= total);
    }


    public void Buy()
    {
        int total = 0;
        foreach (var e in basketDict.Values)
        {
            total += e.TotalPrice;
        }

        if (playerStar < total)
            return;

        foreach (var entry in basketDict.Values)
        {
            if (entry.quantity > 0)
                Debug.Log($"{entry.item.itemName} {entry.quantity}�� ����");
        }

        playerStar -= total;
        Debug.Log($"{playerStar}���� ����");

        foreach (var entry in basketDict.Values)
            entry.quantity = 0;

        foreach (var kvp in slotDict)
            kvp.Value.UpdateDisplay(0);

        UpdateTotalPrice();
    }

    public void Reset()
    {
        foreach (var entry in basketDict.Values)
        {
            entry.quantity = 0;
        }
        foreach (var slot in basketPanelDict.Values)
        {
            Destroy(slot.gameObject);
        }
        basketPanelDict.Clear();

        foreach (var kvp in slotDict)
        {
            kvp.Value.UpdateDisplay(0);
        }

        UpdateTotalPrice();
        foreach (var entry in basketDict.Values)
            buyButton.interactable = (entry.quantity > 0);
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);

        foreach (var entry in basketDict.Values)
        {
            entry.quantity = 0;
        }
        foreach (var slot in basketPanelDict.Values)
        {
            Destroy(slot.gameObject);
        }
        basketPanelDict.Clear();

        foreach (var kvp in slotDict)
        {
            kvp.Value.UpdateDisplay(0);
        }

        UpdateTotalPrice();
        foreach (var entry in basketDict.Values)
            buyButton.interactable = (entry.quantity > 0);
    }



}
