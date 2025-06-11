using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MillManager : MonoBehaviour
{
    public GameObject MillPanel;
    public Transform inventoryPanelParent;
    public GameObject SlotPrefab;
    public SelectedSlot selectedSlot;
    public Sprite[] testIcons;
    public Button confirmButton;

    private MillItemData selectedItem = null;
    private List<MillItemData> Inventory;

    void Start()
    {
        Inventory = new List<MillItemData>
        {
            new MillItemData(/*"쌀",*/ testIcons[0], 3),
            new MillItemData(/*"찹쌀",*/ testIcons[1], 5),
            new MillItemData(/*"단호박",*/ testIcons[2], 2)
        };

        confirmButton.onClick.AddListener(Confirm);
        confirmButton.interactable = false;
        OpenMill();
    }
    public void OpenMill()
    {
        gameObject.SetActive(true);

        foreach (Transform child in inventoryPanelParent)
            Destroy(child.gameObject);

        foreach (var item in Inventory)
        {
            var obj = Instantiate(SlotPrefab, inventoryPanelParent);
            obj.GetComponent<MillInventory>().Setup(item, this);
        }

        selectedItem = null;
        selectedSlot.Clear();
        
    }

  
    public void SelectItem(MillItemData item)
    {
        if (ReferenceEquals(selectedItem, item))
        {
            item.itemQuantity += 1;
            selectedItem = null;
            selectedSlot.Clear();
            UpdateInventoryUI();
            confirmButton.interactable = false;  
            return;
        }

        if (item.itemQuantity <= 0)
            return;

        if (selectedItem != null)
            selectedItem.itemQuantity += 1;

        item.itemQuantity -= 1;
        selectedItem = item;
        selectedSlot.Set(item);
        UpdateInventoryUI();
        confirmButton.interactable = true;  
    }

    private void UpdateInventoryUI()
    {
        foreach (Transform child in inventoryPanelParent)
        {
            var slot = child.GetComponent<MillInventory>();
            slot?.UpdateQuantityText();
        }
    }

    

    public void Confirm()
    {
        /*Debug.Log($"[Confirm 호출] MillManager 인스턴스: {this.GetInstanceID()}");

        Debug.Log("Confirm 눌림 - 선택된 아이템: " + (selectedItem != null ? selectedItem.icon.name : "null"));*/

        /*if (selectedItem == null)
        {
            Debug.Log("선택된 아이템이 없어 Confirm 취소됨");
            return;
        }*/

        Debug.Log($"{selectedItem.icon.name} 아이템을 가루로 만들음");

        selectedItem = null;
        selectedSlot.Clear();
        UpdateInventoryUI();
        confirmButton.interactable = false;
        
    }

    public void CloseMill()
    {
        if (selectedItem != null)
        {
            selectedItem.itemQuantity += 1;
            selectedSlot.Clear();
            selectedItem = null;
            UpdateInventoryUI();
        }
        MillPanel.SetActive(false);
    }
}
