using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class StorageInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image itemImage;
    public TextMeshProUGUI countText;

    //추가: 아이템 정보 저장용
    public string itemName;
    public Sprite itemSprite;
    public string tooltipText;

    public void SetItem(string itemKey, Sprite sprite, int count)
    {
        if (sprite == null)
        {
            ClearSlot(); // 스프라이트가 null이면 슬롯 초기화
            Debug.LogWarning("SetItem: 스프라이트가 null입니다. 슬롯 비움.");
            return;
        }

        itemSprite = sprite;
        itemName = itemKey;
        itemImage.sprite = sprite;
        itemImage.enabled = true;

        // 한글 툴팁 텍스트 매핑
        if (!ItemTooltipDB.TooltipTexts.TryGetValue(itemKey, out tooltipText))
            tooltipText = itemKey; // 혹시 없을 때 대비 예외 처리

        countText.text = (count > 1) ? count.ToString() : "";
        countText.enabled = true;
    }



    public void ClearSlot()
    {
        itemImage.sprite = null;
        itemImage.enabled = false;
        countText.text = "";
        countText.enabled = false;
        itemName = "";
        itemSprite = null;
    }

    public void OnClick()
    {
        if (itemSprite == null || string.IsNullOrEmpty(itemName)) return;
        PlayerStoreBoxInventoryUIManager.Instance.OnItemSelected(itemName, itemSprite);
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
}

public static class ItemTooltipDB
{
    public static Dictionary<string, string> TooltipTexts = new Dictionary<string, string>
    {
        { "Grind_Redbean", "곱게 간 팥" },
        { "Chapssalgaru", "찹쌀가루" },
        { "Mugwortgaru", "쑥 가루" },
        { "Water", "물" },
        { "Danhobakgaru", "단호박가루" },
        { "Konggaru", "콩가루" },
        { "Baeknyeonchogaru", "백년초가루" },
        { "Redbean", "팥" },
        { "Mepssalgaru", "멥쌀가루" },
        { "Mugwort", "쑥" },
        { "Rice", "쌀" },
        { "Mugwort_seedBag", "쑥 씨앗" },
        { "Rice_seedBag", "쌀 모종" }
        // 필요한 만큼 추가
    };
}
