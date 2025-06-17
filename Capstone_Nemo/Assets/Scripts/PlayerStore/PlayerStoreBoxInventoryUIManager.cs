using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStoreBoxInventoryUIManager : MonoBehaviour
{
    public static PlayerStoreBoxInventoryUIManager Instance;

    public GameObject panel;
    public List<StorageInventorySlot> slots;
    public Button closeButton;

    private StorageInventory currentInventory;

    //private string selectedItemName = null;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }

    public void OpenUI(StorageInventory storage)
    {
        // 도감 패널이 열려 있으면 박스 열기/닫기 막기
        if (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen())
            return;

        // 재료 재고 패널이 열려 있으면 박스 열기/닫기 막기
        if (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen())
            return;

        currentInventory = storage;
        panel.SetActive(true);
        //selectedItemName = null;
        UpdateSlots();
    }

    public void CloseUI()
    {
        //panel.SetActive(false);
        //currentInventory = null;
        panel.SetActive(false);
        currentInventory = null;
        if (InventoryTooltipManager.Instance != null)
            InventoryTooltipManager.Instance.Hide();
        //selectedItemName = null;
    }

    public bool IsOpen()
    {
        return panel.activeSelf;
    }

    public void UpdateSlots()
    {
        foreach (var slot in slots)
            slot.ClearSlot();

        int i = 0;
        foreach (var pair in currentInventory.GetAllItems())
        {
            if (i >= slots.Count) break;

            Sprite sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + pair.Key);
            if (sprite == null)
            {
                Debug.LogWarning($"[상자UI] 스프라이트 로드 실패: {pair.Key}");
                continue;
            }

            slots[i].SetItem(pair.Key, sprite, pair.Value);
            i++;
        }
    }

    public void OnItemSelected(string itemName, Sprite sprite)
    {
        // 아무것도 안 들고 있을 때만!
        if (!HeldItemManager.Instance.IsHoldingItem())
        {
            // 재고 있는지 체크
            if (currentInventory.GetItemCount(itemName) > 0)
            {
                currentInventory.AddItem(itemName, -1);
                currentInventory.SaveStorage();
                HeldItemManager.Instance.ShowHeldItem(sprite, itemName);
                Debug.Log($"[인벤토리] {itemName} 1개 소지 시작");
                UpdateSlots();

                SFXManager.Instance.PlayBbyongSFX();
            }
            else
            {
                Debug.LogWarning($"{itemName} 재고가 부족합니다.");
            }
        }
        else
        {
            Debug.Log("이미 다른 아이템을 들고 있음. 먼저 내려놓으세요.");
        }
    }

    private void OnDisable()
    {
        if (InventoryTooltipManager.Instance != null)
            InventoryTooltipManager.Instance.Hide();
    }


    //public void OnItemDeselected(string itemName)
    //{
    //    if (selectedItemName == itemName)
    //    {
    //        selectedItemName = null;
    //        Debug.Log($"[상자UI] 선택 해제: {itemName}");
    //    }
    //}
}
