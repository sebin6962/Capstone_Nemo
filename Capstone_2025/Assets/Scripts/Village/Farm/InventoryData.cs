using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string id;
    public string name;
    public string spritePath;
}

[System.Serializable]
public class ItemList
{
    public List<Item> items;
}

public class InventoryData : MonoBehaviour
{
    public static InventoryData Instance;
    public List<Item> itemList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        TextAsset json = Resources.Load<TextAsset>("InventoryData");
        itemList = JsonUtility.FromJson<ItemList>("{\"items\":" + json.text + "}").items;
    }

    public Item GetItemById(string id)
    {
        return itemList.Find(item => item.id == id);
    }
}
