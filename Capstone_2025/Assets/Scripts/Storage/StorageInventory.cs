using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class StorageEntry
{
    public string name;
    public int amount;
}

[System.Serializable]
public class StorageData
{
    public List<StorageEntry> items = new();
}

public class StorageInventory : MonoBehaviour
{
    public static StorageInventory Instance;

    private Dictionary<string, int> storage = new();
    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            savePath = Path.Combine(Application.persistentDataPath, "storage.json");
            LoadStorage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(string itemName, int amount)
    {
        //if (storage.ContainsKey(itemName))
        //    storage[itemName] += amount;
        //else
        //    storage[itemName] = amount;

        //if (storage.ContainsKey(itemName))
        //{
        //    storage[itemName] += amount;

        //    // ������ 0 ���ϰ� �Ǹ� ����
        //    if (storage[itemName] <= 0)
        //        storage.Remove(itemName);
        //}
        //else if (amount > 0)
        //{
        //    // ���� �߰��� �� amount�� ����� ���� ���
        //    storage[itemName] = amount;
        //}

        if (string.IsNullOrEmpty(itemName)) return;

        if (storage.ContainsKey(itemName))
        {
            storage[itemName] += amount;

            if (storage[itemName] <= 0)
            {
                storage.Remove(itemName);
            }
        }
        else if (amount > 0)
        {
            storage[itemName] = amount;
        }
        else
        {
            Debug.LogWarning($"[StorageInventory] ���� ������ '{itemName}'�� ���� {amount} �߰� �õ�");
        }
    }

    public int GetItemCount(string itemName)
    {
        return storage.TryGetValue(itemName, out var count) ? count : 0;
    }

    public void SaveStorage()
    {
        var data = new StorageData();

        foreach (var pair in storage)
            data.items.Add(new StorageEntry { name = pair.Key, amount = pair.Value });

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log("â�� ���� �Ϸ�");
    }

    public void LoadStorage()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            var data = JsonUtility.FromJson<StorageData>(json);

            storage.Clear();
            foreach (var entry in data.items)
                storage[entry.name] = entry.amount;

            Debug.Log("â�� �ҷ����� �Ϸ�");
        }
        else
        {
            Debug.Log("����� â�� ����. ���� �����մϴ�.");
        }
    }

    public bool HasItem(string itemName)
    {
        return storage.ContainsKey(itemName);
    }

    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(storage);
    }
}

