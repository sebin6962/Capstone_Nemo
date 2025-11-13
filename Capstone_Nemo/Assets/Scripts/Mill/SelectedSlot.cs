using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image countImage;
    [SerializeField] private TMP_Text countText;
    /*[SerializeField] private TMP_Text nameText;*/

    public void Set(MillItemData item, int count)
    {
        icon.sprite = item.icon;
        icon.enabled = true;
        countImage.enabled = (count > 1);
        countText.text = count > 1 ? $"{count}" : "";
        /*nameText.text = item.itemName;*/
    }

    public void UpdateCount(int count)
    {
        countText.text = count > 1 ? $"{count}" : "";
    }

    public void Clear()
    {
        icon.sprite = null;
        countImage.enabled = false;
        icon.enabled = false;
        countText.text = "";
    }
}
