using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultItemUI : MonoBehaviour
{
    public Image image;
    public string itemName;

    public void Initialize(Sprite sprite, string name)
    {
        itemName = name;
        image.sprite = sprite;
    }

    public string GetItemName()
    {
        return itemName;
    }
}
