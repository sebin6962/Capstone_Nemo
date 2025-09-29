using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

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
    //public int playerStar = 100;

    public Transform itemListParent;
    public Transform basketListParent;
    public GameObject itemSlotPrefab;
    public GameObject basketSlotPrefab;

    private Dictionary<ShopData, ShopBasketData> basketDict = new();
    private Dictionary<ShopData, ShopItemSlot> slotDict = new();
    private Dictionary<ShopData, BasketPanelPrefab> basketPanelDict = new();

    private string shopDataPath;

    /*    void Awake()
        {
            shopDataPath = Path.Combine(Application.persistentDataPath, "shopData.json");
            LoadShopData();
        }*/

    public void LoadShopData(string resourcePath)
    {
        TextAsset jsonText = Resources.Load<TextAsset>(resourcePath);
        if (jsonText == null)
        {
            Debug.LogWarning($"{resourcePath}.json 파일을 찾을 수 없음");
            return;
        }

        ShopDataList data = JsonUtility.FromJson<ShopDataList>(jsonText.text);
        ShopItems = data.items;

        Debug.Log($"{ShopItems.Count}개 로드됨 ({resourcePath})");
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        Debug.Log("OpenShop 실행됨");

        /*if (itemSlotPrefab == null) Debug.LogError("itemSlotPrefab이 null입니다.");
        if (itemListParent == null) Debug.LogError("itemListParent가 null입니다.");
        if (ShopItems == null || ShopItems.Count == 0) Debug.LogError("ShopItems가 null이거나 비어있습니다.");*/

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

    //void UpdateTotalPrice()
    //{
    //    int total = 0;
    //    foreach (var e in basketDict.Values)
    //    {
    //        total += e.TotalPrice;
    //    }
    //    totalPriceText.text = $"{total} 별빛";
    //    allTotalPriceText.text = $"총 {total} 별빛";
    //    buyButton.interactable = (total > 0 && playerStar >= total);
    //}

    void UpdateTotalPrice()
    {
        int total = 0;
        foreach (var e in basketDict.Values)
            total += e.TotalPrice;

        // 별빛 데이터는 StarDataManager의 값 사용
        int playerStar = StarDataManager.Instance.playerData.starlight;

        totalPriceText.text = $"{total} 별빛";
        allTotalPriceText.text = $"총 {total} 별빛";
        buyButton.interactable = (total > 0 && playerStar >= total);
    }


    //public void Buy()
    //{
    //    int total = 0;
    //    foreach (var e in basketDict.Values)
    //    {
    //        total += e.TotalPrice;
    //    }

    //    if (playerStar < total)
    //        return;

    //    foreach (var entry in basketDict.Values)
    //    {
    //        if (entry.quantity > 0)
    //            Debug.Log($"{entry.item.itemName} {entry.quantity}개 구매");
    //    }

    //    playerStar -= total;
    //    Debug.Log($"{playerStar}별빛 남음");

    //    foreach (var entry in basketDict.Values)
    //        entry.quantity = 0;

    //    foreach (var kvp in slotDict)
    //        kvp.Value.UpdateDisplay(0);

    //    UpdateTotalPrice();
    //}

    public void Buy()
    {
        int total = 0;
        foreach (var e in basketDict.Values)
            total += e.TotalPrice;

        int playerStar = StarDataManager.Instance.playerData.starlight;
        if (playerStar < total)
            return;

        foreach (var entry in basketDict.Values)
            if (entry.quantity > 0)
            {
                StorageInventory.Instance.AddItem(entry.item.itemName, entry.quantity);
                Debug.Log($"{entry.item.itemName} {entry.quantity}개 구매");
            }

        // 별빛 차감은 StarDataManager를 통해
        StarDataManager.Instance.SpendStarlight(total);

        Debug.Log($"{StarDataManager.Instance.playerData.starlight}별빛 남음");

        foreach (var entry in basketDict.Values)
            entry.quantity = 0;

        foreach (var kvp in slotDict)
            kvp.Value.UpdateDisplay(0);

        UpdateTotalPrice();

        StorageInventory.Instance.SaveStorage();
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
