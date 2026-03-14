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

    private bool isTakenOut = false;

    //튜토리얼잠금용
    private bool isTutorialLocked = false;

    void Update()
    {
        // 스페이스바로 돌려놓기
        if (IsInfiniteSeedSlot() && isTakenOut && Input.GetKeyDown(KeyCode.Space))
        {
            BoxInventoryManager.Instance.RemoveHeldItem();
            itemImage.color = Color.white;
            isTakenOut = false;
        }
    }

    // 무한 슬롯 판별
    public bool IsInfiniteSeedSlot()
    {
        if (BoxInventoryManager.Instance == null) return false;
        int idx = BoxInventoryManager.Instance.slots.IndexOf(this);
        return idx >= 0 && idx < 4; // 0~3번 무한 슬롯
    }

    public void OnClick()
    {
        //튜토리얼 잠금용
        if (isTutorialLocked)
            return;

        bool isHolding = HeldItemManager.Instance.IsHoldingItem();
        string heldName = HeldItemManager.Instance.GetHeldItemName();

        // [무한 슬롯 클릭 처리]
        if (IsInfiniteSeedSlot())
        {
            if (!isHolding && !isTakenOut)
            {
                // 꺼내기
                BoxInventoryManager.Instance.HoldItemFromSlot(GetSprite(), GetItemName());
                itemImage.color = Color.gray;
                isTakenOut = true;
                return;
            }
            else if (isTakenOut)
            {
                // 돌려놓기
                BoxInventoryManager.Instance.RemoveHeldItem();
                itemImage.color = Color.white;
                isTakenOut = false;
                return;
            }
        }

        // [1] 손에 없음 → 슬롯에서 아이템 집기
        if (!isHolding && HasItem())
        {
            if (IsInfiniteSeedSlot())
            {
                // 무한: 수량 줄이지 않음
                BoxInventoryManager.Instance.HoldItemFromSlot(GetSprite(), GetItemName());
                return;
            }

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

        // [2] 손에 아이템 있음
        if (isHolding)
        {
            if (IsInfiniteSeedSlot())
            {
                Debug.Log("[무한 슬롯] 저장 불가");
                return;
            }

            if (ToolData.Instance != null && ToolData.Instance.IsTool(heldName))
            {
                Debug.Log("도구는 저장할 수 없습니다: " + heldName);
                return;
            }

            foreach (var slot in BoxInventoryManager.Instance.slots)
            {
                if (slot.HasItem() && slot.GetItemName() == heldName)
                {
                    slot.SetItem(slot.GetSprite(), heldName, slot.GetItemCount() + 1);
                    BoxInventoryManager.Instance.RemoveHeldItem();
                    BoxInventoryManager.Instance.SaveInventory();
                    return;
                }
            }

            foreach (var slot in BoxInventoryManager.Instance.slots)
            {
                if (!slot.HasItem() && !slot.IsInfiniteSeedSlot())
                {
                    slot.SetItem(BoxInventoryManager.Instance.GetHeldSprite(), heldName, 1);
                    BoxInventoryManager.Instance.RemoveHeldItem();
                    BoxInventoryManager.Instance.SaveInventory();
                    return;
                }
            }
        }
    }

    //튜토리얼잠금용
    public void SetTutorialLocked(bool locked)
    {
        isTutorialLocked = locked;

        if (itemImage == null)
            return;

        if (locked)
        {
            itemImage.color = Color.gray;
        }
        else
        {
            itemImage.color = Color.white;
        }
    }


    public void SetItem(Sprite sprite, string name = "", int count = 1)
    {
        if (sprite == null) return;

        itemImage.sprite = sprite;
        itemImage.enabled = true;
        itemName = string.IsNullOrEmpty(name) ? sprite.name.Replace("(Clone)", "").Trim() : name.Replace("(Clone)", "").Trim();
        this.count = count;

        if (!ItemTooltipDB.TooltipTexts.TryGetValue(name, out tooltipText))
            tooltipText = name;

        // ? 무한 슬롯은 삭제하지 않음
        if (count == 0) { ClearSlot(); return; }

        UpdateUI();
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
            if (count < 0)
                countText.text = "∞"; //  무한대
            else
                countText.text = (count > 1) ? count.ToString() : "";
        }
        else
        {
            countText.text = "";
            countText.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(itemName))
        {
            InventoryTooltipManager.Instance.Show(
                tooltipText,
                GetComponent<RectTransform>()
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryTooltipManager.Instance.Hide();
    }

    public bool HasItem() => itemImage != null && itemImage.sprite != null;
    public string GetItemName() => itemName;
    public int GetItemCount() => count;
    public Sprite GetSprite() => itemImage.sprite;
}

