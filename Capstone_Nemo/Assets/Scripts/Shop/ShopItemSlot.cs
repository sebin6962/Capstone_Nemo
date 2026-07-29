using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text ownedText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Image itemImage;
    [SerializeField] private GameObject alarmImage;
    

    private ShopData item;
    private ShopManager manager;

    public void Setup(ShopData newItem, ShopManager shopManager, int ownedCount)
    {
        item = newItem;
        manager = shopManager;

        string displayName;
        if (!ItemTooltipDB.TooltipTexts.TryGetValue(item.itemName, out displayName))
            displayName = item.itemName; 

        itemNameText.text = displayName;
        priceText.text = item.price + " 별빛";

        Sprite sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + item.itemName);
        if (sprite != null)
        {
            itemImage.sprite = sprite;
            itemImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[ShopItemSlot] 스프라이트 로드 실패: {item.itemName}");
            itemImage.enabled = false;
        }

        plusButton.onClick.AddListener(OnPlusButtonClicked);
        minusButton.onClick.AddListener(OnMinusButtonClicked);

        UpdateDisplay(0);
        UpdateOwnedDisplay(ownedCount);
    }

    public void SetAlarmImage(bool isNew)
    {
        if (alarmImage != null)
            alarmImage.SetActive(isNew);
    }

    void OnPlusButtonClicked()
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBtnClickSFX();
        manager.AdjustItem(item, +1);
    }

    void OnMinusButtonClicked()
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBtnClickSFX();
        manager.AdjustItem(item, -1);
    }

    public void UpdateDisplay(int quantity)
    {
        quantityText.text = $"{quantity}";
    }

    public void UpdateOwnedDisplay(int ownedCount)
    {
        if (ownedText != null)
            ownedText.text = $"{ownedCount}";
    }
}
