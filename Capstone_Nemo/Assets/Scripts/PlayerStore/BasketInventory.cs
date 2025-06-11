using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class BasketInventory : MonoBehaviour
{
    public int capacity = 2;
    public List<string> itemNames = new();
    public List<Sprite> itemSprites = new();

    [HideInInspector] public Vector3 originalPosition;

    private string SavePath => Application.persistentDataPath + "/basket_inventory.json";

    private void Awake()
    {
        Load();
    }

    public bool AddItem(string name, Sprite sprite)
    {
        HashSet<string> uniqueItems = new(itemNames);
        bool isNewItem = !uniqueItems.Contains(name);
        if (isNewItem && uniqueItems.Count >= capacity)
            return false;

        itemNames.Add(name);
        itemSprites.Add(sprite);
        Save();
        return true;
    }

    public void Clear()
    {
        itemNames.Clear();
        itemSprites.Clear();
        Save(); // 저장
    }

    public void Save()
    {
        BasketData data = new BasketData();
        data.itemNames = new List<string>(itemNames);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("[바구니 저장] " + SavePath);
    }

    public void Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            BasketData data = JsonUtility.FromJson<BasketData>(json);
            itemNames = data.itemNames;

            itemSprites.Clear();
            foreach (string name in itemNames)
            {
                Sprite sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + name);
                if (sprite != null)
                    itemSprites.Add(sprite);
                else
                    Debug.LogWarning($"[로드 실패] 스프라이트 없음: {name}");
            }
        }
    }
}

[System.Serializable]
public class BasketData
{
    public List<string> itemNames = new();
}

