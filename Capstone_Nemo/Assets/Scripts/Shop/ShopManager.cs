using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    public GameObject shopPanel;
    public TMP_Text itemNameText;
    public TMP_Text totalPriceText;
    public TMP_Text allTotalPriceText;
    public Button buyButton;
    public Button resetButton;
    public Button closeButton;
    public List<ShopData> ShopItems;

    private HashSet<string> seenItems = new();
    private string seenPrefsKey;

    public Transform itemListParent;
    public Transform basketListParent;
    public GameObject itemSlotPrefab;
    public GameObject basketSlotPrefab;

    private Dictionary<ShopData, ShopBasketData> basketDict = new();
    private Dictionary<ShopData, ShopItemSlot> slotDict = new();
    private Dictionary<ShopData, BasketPanelPrefab> basketPanelDict = new();

    private string shopDataPath;

    private bool isMillShop = false;

    [System.Serializable]
    class ShopSeenData
    {
        public List<string> seenItems = new();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

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

        //방앗간판정
        isMillShop = resourcePath.ToLower().Contains("mill");

        Debug.Log($"{ShopItems.Count}개 로드됨 ({resourcePath})");
    }

    string GetServerName()
    {
        return PlayerPrefs.GetString("SelectedSave", "default");
    }

    void LoadSeenItems()
    {
        string server = GetServerName();
        seenPrefsKey = $"{server}:ShopSeen";

        seenItems = new HashSet<string>();
        string json = PlayerPrefs.GetString(seenPrefsKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            var data = JsonUtility.FromJson<ShopSeenData>(json);
            if (data != null && data.seenItems != null)
            {
                foreach (var k in data.seenItems)
                    seenItems.Add(k);
            }
        }
    }

    void SaveSeenItems()
    {
        var data = new ShopSeenData
        {
            seenItems = new List<string>(seenItems)
        };
        PlayerPrefs.SetString(seenPrefsKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void OpenShop()
    {
        // 도감 패널이 열려 있으면 창고 열기/닫기 막기
        if (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen())
            return;

        // 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;

        if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
        shopPanel.SetActive(true);
        Debug.Log("OpenShop 실행됨");

        /*if (itemSlotPrefab == null) Debug.LogError("itemSlotPrefab이 null입니다.");
        if (itemListParent == null) Debug.LogError("itemListParent가 null입니다.");
        if (ShopItems == null || ShopItems.Count == 0) Debug.LogError("ShopItems가 null이거나 비어있습니다.");*/

        if (seenItems == null || seenItems.Count == 0)
            LoadSeenItems(); 

        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        slotDict.Clear();
        basketDict.Clear();

        foreach (var item in ShopItems)
        {
            //해금
            bool unlocked = true;

            if (UnlockManager.Instance != null)
            {
                unlocked = UnlockManager.Instance.IsShopItemUnlocked(item.itemName, isMillShop);
            }

            if (!unlocked)
            {
                continue;
            }

            string key = item.itemName;

            bool isNew = !seenItems.Contains(key);

            var slotObj = Instantiate(itemSlotPrefab, itemListParent);
            var slot = slotObj.GetComponent<ShopItemSlot>();
            slot.Setup(item, this);

            slot.SetAlarmImage(isNew);

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

        totalPriceText.text = $"{total}";
        allTotalPriceText.text = $"{total}";
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

                if (StorageAlertManager.Instance != null)
                {
                    StorageAlertManager.Instance.NotifyNewHarvestedItem(entry.item.itemName);
                }
            }

        // 별빛 차감은 StarDataManager를 통해
        StarDataManager.Instance.SpendStarlight(total);
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayTotalMoneySFX();
            SFXManager.Instance.PlayFileSelectSFX();
        }

        Debug.Log($"{StarDataManager.Instance.playerData.starlight}별빛 남음");

        foreach (var entry in basketDict.Values)
            entry.quantity = 0;

        foreach (var kvp in slotDict)
            kvp.Value.UpdateDisplay(0);

        UpdateTotalPrice();

        StorageInventory.Instance.SaveStorage();

        Reset();
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

        if (SFXManager.Instance) SFXManager.Instance.PlayFileSelectSFX();
        UpdateTotalPrice();
        foreach (var entry in basketDict.Values)
            buyButton.interactable = (entry.quantity > 0);
    }

    public void CloseShop()
    {
        SFXManager.Instance.PlayBbyongSFX();
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

        foreach (var item in ShopItems)
        {
            if (UnlockManager.Instance != null &&
                !UnlockManager.Instance.IsShopItemUnlocked(item.itemName, isMillShop))
                continue;

            string key = item.itemName;
            seenItems.Add(key);
        }
        SaveSeenItems();
    }

    public bool IsOpen()
    {
        return shopPanel != null && shopPanel.activeSelf;
    }

}
