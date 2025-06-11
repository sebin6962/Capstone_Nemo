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
            new MillItemData(/*"��",*/ testIcons[0], 3),
            new MillItemData(/*"����",*/ testIcons[1], 5),
            new MillItemData(/*"��ȣ��",*/ testIcons[2], 2)
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
        /*Debug.Log($"[Confirm ȣ��] MillManager �ν��Ͻ�: {this.GetInstanceID()}");

        Debug.Log("Confirm ���� - ���õ� ������: " + (selectedItem != null ? selectedItem.icon.name : "null"));*/

        /*if (selectedItem == null)
        {
            Debug.Log("���õ� �������� ���� Confirm ��ҵ�");
            return;
        }*/

        Debug.Log($"{selectedItem.icon.name} �������� ����� ������");

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
