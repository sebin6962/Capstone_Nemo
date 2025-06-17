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
        // ���� �г��� ���� ������ �ڽ� ����/�ݱ� ����
        if (DoGamUIManager.Instance != null && DoGamUIManager.Instance.IsOpen())
            return;

        // ��� ��� �г��� ���� ������ �ڽ� ����/�ݱ� ����
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
                Debug.LogWarning($"[����UI] ��������Ʈ �ε� ����: {pair.Key}");
                continue;
            }

            slots[i].SetItem(pair.Key, sprite, pair.Value);
            i++;
        }
    }

    public void OnItemSelected(string itemName, Sprite sprite)
    {
        // �ƹ��͵� �� ��� ���� ����!
        if (!HeldItemManager.Instance.IsHoldingItem())
        {
            // ��� �ִ��� üũ
            if (currentInventory.GetItemCount(itemName) > 0)
            {
                currentInventory.AddItem(itemName, -1);
                currentInventory.SaveStorage();
                HeldItemManager.Instance.ShowHeldItem(sprite, itemName);
                Debug.Log($"[�κ��丮] {itemName} 1�� ���� ����");
                UpdateSlots();

                SFXManager.Instance.PlayBbyongSFX();
            }
            else
            {
                Debug.LogWarning($"{itemName} ��� �����մϴ�.");
            }
        }
        else
        {
            Debug.Log("�̹� �ٸ� �������� ��� ����. ���� ������������.");
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
    //        Debug.Log($"[����UI] ���� ����: {itemName}");
    //    }
    //}
}
