using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class InventoryTooltipManager : MonoBehaviour
{
    public static InventoryTooltipManager Instance;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    // 나무(월드) 툴팁이 떠 있는지 여부
    private bool isWorldTooltipVisible = false;

    private void Awake()
    {
        Instance = this;
        tooltipPanel.SetActive(false);

        // 툴팁 패널은 레이캐스트 막지 않도록
        var cg = tooltipPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // 텍스트도 raycastTarget 끄기
        if (tooltipText != null)
            tooltipText.raycastTarget = false;
    }

    public void Show(string text, RectTransform slotRect)
    {
        tooltipPanel.SetActive(true);
        tooltipText.text = text;

        // 1. 슬롯의 월드 포지션을 스크린 포지션으로 변환
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, slotRect.position);

        // 2. 툴팁 패널의 부모(Canvas)의 RectTransform 기준으로 로컬 좌표 변환
        RectTransform canvasRect = tooltipPanel.transform.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, Camera.main, out localPoint
        );

        // 3. 슬롯 위쪽(y값)으로 60픽셀 올리기 (적당히 조절)
        tooltipPanel.GetComponent<RectTransform>().anchoredPosition = localPoint + new Vector2(0, slotRect.rect.height / 2 + 30f);
    }


    public void Hide()
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(false);
    }

    //==========작물 나무용 툴팁 때문에 추가한 월드 좌표 받는 오버로드==============
    public void ShowWorld(string text, Vector3 worldPos)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        isWorldTooltipVisible = true;

        tooltipPanel.SetActive(true);
        tooltipText.text = text;

        Camera cam = Camera.main;
        if (cam == null) return;

        // 월드 → 스크린
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        // 캔버스 기준 로컬 좌표로 변환
        RectTransform canvasRect = tooltipPanel.transform.parent as RectTransform;
        if (canvasRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            cam,
            out localPoint
        );

        // 나무 위에 살짝 띄워서 배치
        RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = localPoint + new Vector2(0f, 60f);
    }

    // 나무용 툴팁 숨기기
    public void HideWorld()
    {
        isWorldTooltipVisible = false;
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(false);
    }
}