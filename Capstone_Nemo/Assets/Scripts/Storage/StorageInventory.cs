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
            DontDestroyOnLoad(gameObject);
            // savePath는 SetServerName에서 할당
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetServerName(string serverName)
    {
        savePath = Path.Combine(Application.persistentDataPath, $"storage_{serverName}.json");
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

        //    // 수량이 0 이하가 되면 제거
        //    if (storage[itemName] <= 0)
        //        storage.Remove(itemName);
        //}
        //else if (amount > 0)
        //{
        //    // 새로 추가할 땐 amount가 양수일 때만 허용
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
            Debug.LogWarning($"[StorageInventory] 없는 아이템 '{itemName}'에 음수 {amount} 추가 시도");
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
    }

    public void LoadStorage()
    {
        Debug.Log("[StorageInventory] 실제 로드 경로: " + savePath);

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            var data = JsonUtility.FromJson<StorageData>(json);
            storage.Clear();
            foreach (var entry in data.items)
                storage[entry.name] = entry.amount;
        }
        else
        {
            Debug.LogWarning("[StorageInventory] 파일 없음! 초기화!");
            storage.Clear();
            SaveStorage();
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

    public void LoadFromSaveData(List<StorageEntry> entries)
    {
        storage.Clear();
        if (entries == null) return;
        foreach (var entry in entries)
            storage[entry.name] = entry.amount;
    }
}

