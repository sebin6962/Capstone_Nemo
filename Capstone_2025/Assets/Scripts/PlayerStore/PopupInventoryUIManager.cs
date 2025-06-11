using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PopupInventoryUIManager : MonoBehaviour
{
    public static PopupInventoryUIManager Instance;

    public GameObject panel;
    public List<StorageInventorySlot> slots;

    public TextMeshProUGUI titleText;
    public GameObject closeButton;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenPopup(string title = "상자 인벤토리")
    {
        titleText.text = title;
        UpdateSlots();
        panel.SetActive(true);
        closeButton.SetActive(true);
    }

    public void ClosePopup()
    {
        panel.SetActive(false);
        closeButton.SetActive(false);
    }

    public bool IsPopupOpen()
    {
        return panel.activeSelf;
    }

    public void UpdateSlots()
    {
        foreach (var slot in slots)
            slot.ClearSlot();

        int i = 0;
        foreach (var pair in StorageInventory.Instance.GetAllItems())
        {
            if (i >= slots.Count) break;

            Sprite sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + pair.Key);
            if (sprite == null)
            {
                Debug.LogWarning($"[팝업] 스프라이트 로드 실패: {pair.Key}");
                continue;
            }

            slots[i].SetItem(pair.Key, sprite, pair.Value);
            i++;
        }
    }
}