using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopData
{
    public string itemName;
    public int price;
    public Sprite icon;
}

[System.Serializable]
public class ShopDataList
{
    public List<ShopData> items = new();
}
