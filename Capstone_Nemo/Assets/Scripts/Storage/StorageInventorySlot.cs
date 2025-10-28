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
        {"HotWater", "뜨거운 물" },
        { "Danhobak", "단호박" },
        { "Danhobakgaru", "단호박가루" },
        { "Danhobakgaru_Bag", "단호박씨앗" },
        { "Konggaru", "콩가루" },
        { "Baeknyeoncho", "백년초" },
        { "Baeknyeonchogaru", "백년초가루" },
        { "Baeknyeonchogaru_Bag", "백년초씨앗" },
        { "Redbean", "팥" },
        { "Mepssalgaru", "멥쌀가루" },
        { "Mugwort", "쑥" },
        { "Rice", "쌀" },
        { "Mugwort_seedBag", "쑥 씨앗" },
        { "Rice_seedBag", "쌀 모종" },
        { "Baekseolgi_finish", "백설기" },
        { "Danhobakseolgi_finish", "단호박설기" },
        { "Rainbowseolgi_finish", "무지개설기" },
        { "Injeolmi_finish", "인절미" },
        { "MugwortInjeolmi_finish", "쑥인절미" },
        { "Kkultteok_finish", "꿀떡" },
        { "Songpyeon_finish", "송편" },
        { "Chapssaltteok_finish", "찹쌀떡" },
        { "Jeolpyeon_finish", "절편" },
        { "Jeonggwa_Apple_finish", "사과정과" },
        { "Jeonggwa_Geumgyul_finish", "금귤정과" },
        { "Gyeongdan_finish", "경단" },
        { "Manggaetteok_finish", "망개떡" },
        { "Yakgwa_finish", "약과" },
        { "Baramtteok_finish", "바람떡" },
        { "Gaesungjuak_finish", "개성주악" },
        { "Sanja_finish", "산자" },
        { "Sikhye_finish", "식혜" },
        { "Yujacha_finish", "유자차" },
        { "Mogwacha_finish", "모과차" },
        { "Omijacha_finish", "오미자차" },
        { "Sujeonggwa_finish", "수정과" },
        { "Chamgireum", "참기름" },
        { "Honey", "꿀" },
        { "Kkae", "깨" },
        { "Jat", "잣" },
        { "Powder", "녹말가루" },
        { "Sugar_white", "설탕" },
        { "cinnamon", "계피" },
        { "Cinamongaru", "계피가루" },
        { "Ginger", "생강" },
        { "Sugar_brown", "흑설탕" },
        { "Gotgam", "곶감" },
        { "Omija", "오미자" },
        { "Flour", "밀가루" },
        { "Alcohol", "술" },
        { "FriedRice", "쌀튀밥" },
        { "malt", "엿기름" },
        {"Apple", "사과" },
        {"Cutting_Apple", "자른 사과" },
        {"Yuja", "유자" },
        {"Cutting_Yuja", "자른 유자" },
        {"Geumgyul", "금귤" },
        {"Cutting_Geumgyul", "자른 금귤" },
        {"Mogwa", "모과" },
        {"Cutting_Mogwa", "자른 모과" },
        {"FlowerBouquet_finish", "달꽃 부케" },
        {"FailRiceCake_finish", "다과..?" }
        // 필요한 만큼 추가
    };
}
