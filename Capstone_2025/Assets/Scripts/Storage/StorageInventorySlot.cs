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

    //�߰�: ������ ���� �����
    public string itemName;
    public Sprite itemSprite;
    public string tooltipText;

    public void SetItem(string itemKey, Sprite sprite, int count)
    {
        if (sprite == null)
        {
            ClearSlot(); // ��������Ʈ�� null�̸� ���� �ʱ�ȭ
            Debug.LogWarning("SetItem: ��������Ʈ�� null�Դϴ�. ���� ���.");
            return;
        }

        itemSprite = sprite;
        itemName = itemKey;
        itemImage.sprite = sprite;
        itemImage.enabled = true;

        // �ѱ� ���� �ؽ�Ʈ ����
        if (!ItemTooltipDB.TooltipTexts.TryGetValue(itemKey, out tooltipText))
            tooltipText = itemKey; // Ȥ�� ���� �� ��� ���� ó��

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
                tooltipText, // ������ �� �ؽ�Ʈ
                GetComponent<RectTransform>() // ���� RectTransform
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
        { "Grind_Redbean", "���� �� ��" },
        { "Chapssalgaru", "���Ұ���" },
        { "Mugwortgaru", "�� ����" },
        { "Water", "��" },
        { "Danhobakgaru", "��ȣ�ڰ���" },
        { "Konggaru", "�ᰡ��" },
        { "Baeknyeonchogaru", "����ʰ���" },
        { "Redbean", "��" },
        { "Mepssalgaru", "��Ұ���" },
        { "Mugwort", "��" },
        { "Rice", "��" },
        { "Mugwort_seedBag", "�� ����" },
        { "Rice_seedBag", "�� ����" }
        // �ʿ��� ��ŭ �߰�
    };
}
