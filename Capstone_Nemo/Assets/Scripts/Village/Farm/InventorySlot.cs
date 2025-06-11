using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image itemImage;
    public TextMeshProUGUI countText;

    private string itemName;
    private int count;

    public string tooltipText;

    public void OnClick()
    {
        bool isHolding = HeldItemManager.Instance.IsHoldingItem();
        string heldName = HeldItemManager.Instance.GetHeldItemName();

        // [1] �տ� ���� �� ���Կ��� ������ ����
        if (!isHolding && HasItem())
        {
            if (GetItemCount() > 1)
            {
                BoxInventoryManager.Instance.HoldItemFromSlot(GetSprite(), GetItemName());
                SetItem(GetSprite(), GetItemName(), GetItemCount() - 1);
            }
            else
            {
                BoxInventoryManager.Instance.PickUpFromSlot(this);
            }

            BoxInventoryManager.Instance.SaveInventory();
            return;
        }

        // [2] �տ� ������ ���� �� �κ��丮�� �ڵ�����ó�� ó��
        if (isHolding)
        {
            // [2-1] ������ ���� �Ұ�
            if (ToolData.Instance != null && ToolData.Instance.IsTool(heldName))
            {
                Debug.Log("������ ������ �� �����ϴ�: " + heldName);
                return;
            }

            // [2-2] ���� ���Կ� �ִ� ��� ���� +1
            foreach (var slot in BoxInventoryManager.Instance.slots)
            {
                if (slot.HasItem() && slot.GetItemName() == heldName)
                {
                    slot.SetItem(slot.GetSprite(), heldName, slot.GetItemCount() + 1);
                    BoxInventoryManager.Instance.RemoveHeldItem();
                    BoxInventoryManager.Instance.SaveInventory();
                    Debug.Log("���� Ŭ��: ���� ���Կ� �ڵ� �߰���");
                    return;
                }
            }

            // [2-3] �� ���Կ� ����
            foreach (var slot in BoxInventoryManager.Instance.slots)
            {
                if (!slot.HasItem())
                {
                    slot.SetItem(BoxInventoryManager.Instance.GetHeldSprite(), heldName, 1);
                    BoxInventoryManager.Instance.RemoveHeldItem();
                    BoxInventoryManager.Instance.SaveInventory();
                    Debug.Log("���� Ŭ��: �� ���Կ� �����");
                    return;
                }
            }

            Debug.Log("���� Ŭ��: ���� ���� - ���� ����");
        }
    }

    public void SetItem(Sprite sprite, string name = "", int count = 1)
    {
        if (sprite == null)
        {
            Debug.LogWarning("SetItem: null ��������Ʈ ���޵�!");
            return;
        }

        itemImage.sprite = sprite;
        itemImage.enabled = true;
        itemName = string.IsNullOrEmpty(name) ? sprite.name.Replace("(Clone)", "").Trim() : name.Replace("(Clone)", "").Trim();
        this.count = count;

        // �ѱ� ���� �ؽ�Ʈ ����
        if (!ItemTooltipDB.TooltipTexts.TryGetValue(name, out tooltipText))
            tooltipText = name; // Ȥ�� ���� �� ��� ���� ó��

        if (count <= 0)
        {
            ClearSlot(); // ���� ����
            return;
        }

        UpdateUI();

        Debug.Log($"���Կ� ������ ������: {itemName} x{count}");
    }

    public void ClearSlot()
    {
        itemImage.sprite = null;
        itemImage.enabled = false;
        itemName = "";
        count = 0;

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (countText == null) return;

        if (HasItem())
        {
            countText.enabled = true;
            countText.text = (count > 1) ? count.ToString() : "";
        }
        else
        {
            countText.text = "";
            countText.enabled = false; // ������ ����� �� ����
        }
        //if (countText != null)
        //{
        //    countText.text = (count > 1) ? count.ToString() : "";
        //}
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(itemName))
        {
            InventoryTooltipManager.Instance.Show(
                tooltipText, // ������ �� �ؽ�Ʈ
                GetComponent<RectTransform>() // ���� RectTransform
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryTooltipManager.Instance.Hide();
    }
    public bool HasItem()
    {
        return itemImage != null && itemImage.sprite != null;
    }

    public string GetItemName() => itemName;
    public int GetItemCount() => count;
    public Sprite GetSprite() => itemImage.sprite;
}
