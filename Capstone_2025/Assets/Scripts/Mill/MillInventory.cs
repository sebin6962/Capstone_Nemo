using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MillInventory : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text quantityText;

    private MillItemData itemData;
    private MillManager millManager;

    public void Setup(MillItemData data, MillManager manager)
    {
        /*Debug.Log("Setup »£√‚µ : " + data.icon.name);*/

        itemData = data;
        millManager = manager;

        icon.sprite = data.icon;
        UpdateQuantityText();

    }

    public void OnClick()
    {
        millManager.SelectItem(itemData);
    }

    public void UpdateQuantityText()
    {
        quantityText.text = itemData.itemQuantity.ToString();
    }
}
