using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class InventoryTooltipManager : MonoBehaviour
{
    public static InventoryTooltipManager Instance;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    private void Awake()
    {
        Instance = this;
        tooltipPanel.SetActive(false);
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
}
