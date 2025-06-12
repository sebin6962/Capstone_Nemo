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

        // 1. ������ ���� �������� ��ũ�� ���������� ��ȯ
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, slotRect.position);

        // 2. ���� �г��� �θ�(Canvas)�� RectTransform �������� ���� ��ǥ ��ȯ
        RectTransform canvasRect = tooltipPanel.transform.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, Camera.main, out localPoint
        );

        // 3. ���� ����(y��)���� 60�ȼ� �ø��� (������ ����)
        tooltipPanel.GetComponent<RectTransform>().anchoredPosition = localPoint + new Vector2(0, slotRect.rect.height / 2 + 30f);
    }


    public void Hide()
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(false);
    }
}
