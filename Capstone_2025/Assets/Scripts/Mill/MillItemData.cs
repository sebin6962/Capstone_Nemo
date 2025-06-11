using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class MillItemData
{
    /*public string itemName;*/
    public Sprite icon;
    public int itemQuantity;

    public MillItemData(/*string name,*/ Sprite iconSprite, int quantity)
    {
        /*itemName = name;*/
        icon = iconSprite;
        this.itemQuantity = quantity;
    }
}
