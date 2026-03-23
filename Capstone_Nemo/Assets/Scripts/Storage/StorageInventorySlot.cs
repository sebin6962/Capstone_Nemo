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

    // 수량 텍스트 뒤 배경
    public GameObject countBackground;

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
        //if (!ItemTooltipDB.TooltipTexts.TryGetValue(itemKey, out tooltipText))
        //    tooltipText = itemKey; // 혹시 없을 때 대비 예외 처리

        var key = itemKey.Trim();

        if (!ItemTooltipDB.TooltipTexts.TryGetValue(key, out tooltipText) &&
            !ItemTooltipDB.TooltipTexts.TryGetValue(key.ToLower(), out tooltipText))
        {
            tooltipText = itemKey; // 그래도 없으면 영어 출력
        }

        //원래 수량 처리
        //countText.text = (count > 1) ? count.ToString() : "";
        //countText.enabled = true;

        // 여기부터 수량/배경 처리
        if (count > 1)
        {
            countText.text = count.ToString();
            countText.enabled = true;

            if (countBackground != null)
                countBackground.SetActive(true);   // 2개 이상일 때만 ON
        }
        else
        {
            countText.text = "";
            countText.enabled = false;

            if (countBackground != null)
                countBackground.SetActive(false);  // 1개 이하일 때 OFF
        }
    }



    public void ClearSlot()
    {
        itemImage.sprite = null;
        itemImage.enabled = false;
        countText.text = "";
        countText.enabled = false;
        itemName = "";
        itemSprite = null;

        if (countBackground != null)
            countBackground.SetActive(false);   // 비어있을 때는 항상 끄기
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
    public static Dictionary<string, string> TooltipTexts = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Grind_Redbean", "곱게 간 팥" },
        { "Chapssalgaru", "찹쌀가루" },
        { "Mugwortgaru", "쑥가루" },
        { "Water", "물" },
        {"HotWater", "뜨거운 물" },
        { "Danhobak", "단호박" },
        { "Danhobakgaru", "단호박가루" },
        { "Danhobak_seedBag", "단호박씨앗" },
        { "Mixing_Danhobak", "단호박반죽" },
        { "Pot_Geumgyul", "끓인 금귤" },
        { "Siru_Apple", "익힌 사과" },
        
        { "Konggaru", "콩가루" },
        { "Baeknyeoncho", "백년초" },
        { "Baeknyeonchogaru", "백년초가루" },
        { "Baeknyeoncho_seedBag", "백년초씨앗" },
        {"Mixing_Baeknyeoncho", "백년초반죽" },
        { "Redbean", "팥" },
        { "Mepssalgaru", "멥쌀가루" },
        { "Sieve_Mepssalgaru", "체 친 멥쌀가루" },
        { "Mixing_Mepssal_Hot", "멥쌀 익반죽" },
        { "Mixing_Mepssal", "멥쌀 반죽" },
        { "Sieve_Chapssalgaru", "체 친 찹쌀가루" },
        { "Mixing_Chapssal_Hot", "찹쌀 익반죽" },
        { "Mixing_Chapssal", "찹쌀 반죽" },       
        {"Mixing_Mugwort", "쑥 반죽" },
        {"Injeolmibanjuk", "인절미 반죽" },
        {"Baramtteokbanjuk", "바람떡 반죽" },
        {"Chapssaltteokbanjuk", "찹쌀떡 반죽" },
        {"Gyeongdanbanjuk", "경단 반죽" },
        {"Songpyeonbanjuk", "송편 반죽" },
        {"YakgwaMold", "약과 틀" },
        {"Pot_Gyeongdanbanjuk", "데친 경단 반죽" },
        {"Sanjabanjuk", "산자 반죽" },
        {"Honey_Sanjabanjuk", "꿀 묻힌 산자 반죽" },
        { "kkultteokbanjuk", "꿀떡 반죽" },
        { "Gaesungjuakbanjuk", "개성주악 반죽" },
        { "Frying_Gaesungjuakbanjuk", "튀긴 개성주악 반죽" },
        { "Frying_Sanjabanjuk", "튀긴 산자 반죽" },
        { "Frying_Yakgwabanjuk", "튀긴 약과 반죽" },
        { "Mugwort", "쑥" },
        { "Rice", "쌀" },
        { "Mugwort_seedBag", "쑥씨앗" },
        { "Rice_seedBag", "쌀모종" },
        { "Baekseolgi_finish", "백설기" },
        { "Danhobakseolgi_finish", "단호박설기" },
        { "Rainbowseolgi_finish", "무지개설기" },
        { "Injeolmi_finish", "인절미" },
        { "Redbeansiru_finish", "팥시루떡" },
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
        { "Chung_Yuja", "유자청" },
        { "Mogwacha_finish", "모과차" },
        { "Chung_Mogwa", "모과청" },
        { "Chung_Omija", "오미자청" },
        { "Omijacha_finish", "오미자차" },
        { "Omijacha", "미완성 오미자차" },
        { "Sujeonggwa_finish", "수정과" },
        { "Sujeonggwa", "미완성 수정과" },
        { "Chamgireum", "참기름" },
        { "Honey", "꿀" },
        { "Kkae", "깨" },
        { "Mixing_Kkuulkkaeso", "꿀깨소" },
        { "Jat", "잣" },
        { "Powder", "녹말가루" },
        { "Sugar_white", "설탕" },
        { "Cinnamon", "계피" },
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
        {"Sugar_Apple", "설탕사과절임" },
        {"Yuja", "유자" },
        {"Cutting_Yuja", "자른 유자" },
        {"Geumgyul", "금귤" },
        {"Cutting_Geumgyul", "자른 금귤" },
        {"Mogwa", "모과" },
        {"Cutting_Mogwa", "자른 모과" },
        {"FlowerBouquet_finish", "달꽃부케" },
        {"FailRiceCake_finish", "다과..?" },
        {"Manggaeleaf", "망개잎" },
        {"Jeolgu", "방앗간" },
        {"MixingBowl", "반죽대" },
        {"Pot", "냄비" },
        {"Sieve", "체" },
        {"Siru", "시루" },
        {"ShapeMaker", "빚기" },
        {"Sink", "개수대" },
        {"Cutting", "자르기" },
        {"ChungMaker", "청 작업대" },
        {"Deco", "마무리 작업대" },
        {"Drying", "건조기" },
        {"Fryolator", "튀김기" },
        {"Grinder", "절구" },
        
        // 필요한 만큼 추가
    };
}
