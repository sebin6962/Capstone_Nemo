using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    /*[SerializeField] private TMP_Text nameText;*/

    public void Set(MillItemData item)
    {
        icon.sprite = item.icon;
        /*nameText.text = item.itemName;*/
    }

    public void Clear()
    {
        icon.sprite = null;
        /*nameText.text = "";*/
    }
}
