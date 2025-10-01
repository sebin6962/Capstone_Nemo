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

        // [1] 손에 없음 → 슬롯에서 아이템 집기
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

        // [2] 손에 아이템 있음 → 인벤토리에 자동저장처럼 처리
        if (isHolding)
        {
            // [2-1] 도구는 저장 불가
            if (ToolData.Instance != null && ToolData.Instance.IsTool(heldName))
            {
                Debug.Log("도구는 저장할 수 없습니다: " + heldName);
                return;
            }

            // [2-2] 기존 슬롯에 있는 경우 수량 +1
            foreach (var slot in BoxInventoryManager.Instance.slots)
            {
                if (slot.HasItem() && slot.GetItemName() == heldName)
                {
                    slot.SetItem(slot.GetSprite(), heldName, slot.GetItemCount() + 1);
                    BoxInventoryManager.Instance.RemoveHeldItem();
                    BoxInventoryManager.Instance.SaveInventory();
                    Debug.Log("슬롯 클릭: 기존 슬롯에 자동 추가됨");
                    return;
                }
            }

            // [2-3] 빈 슬롯에 저장
            foreach (var slot in BoxInventoryManager.Instance.slots)
            {
                if (!slot.HasItem())
                {
                    slot.SetItem(BoxInventoryManager.Instance.GetHeldSprite(), heldName, 1);
                    BoxInventoryManager.Instance.RemoveHeldItem();
                    BoxInventoryManager.Instance.SaveInventory();
                    Debug.Log("슬롯 클릭: 빈 슬롯에 저장됨");
                    return;
                }
            }

            Debug.Log("슬롯 클릭: 저장 실패 - 슬롯 부족");
        }
    }

    public void SetItem(Sprite sprite, string name = "", int count = 1)
    {
        if (sprite == null)
        {
            Debug.LogWarning("SetItem: null 스프라이트 전달됨!");
            return;
        }

        itemImage.sprite = sprite;
        itemImage.enabled = true;
        itemName = string.IsNullOrEmpty(name) ? sprite.name.Replace("(Clone)", "").Trim() : name.Replace("(Clone)", "").Trim();
        this.count = count;

        // 한글 툴팁 텍스트 매핑
        if (!ItemTooltipDB.TooltipTexts.TryGetValue(name, out tooltipText))
            tooltipText = name; // 혹시 없을 때 대비 예외 처리

        if (count <= 0)
        {
            ClearSlot(); // 완전 제거
            return;
        }

        UpdateUI();

        Debug.Log($"슬롯에 아이템 설정됨: {itemName} x{count}");
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
            countText.enabled = false; // 슬롯이 비었을 땐 숨김
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
                tooltipText, // 툴팁에 쓸 텍스트
                GetComponent<RectTransform>() // 슬롯 RectTransform
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
