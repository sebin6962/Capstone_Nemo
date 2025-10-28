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

    public int maxSlots = 12;          // 서로 다른 아이템 종류 수(슬롯) 최대치
    public int maxStackPerItem = 99;   // 아이템 1종류당 최대 수량 (0이면 무제한)

    public int OccupiedSlots => storage.Count;                 // 사용 중 슬롯 수
    public int FreeSlots => Mathf.Max(0, maxSlots - storage.Count); // 남은 슬롯 수

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
            AddItem("Mepssalgaru", 10);
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

    public bool HasRoomFor(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0) return true;

        // 같은 아이템이면: 99 초과 금지 (슬롯 가득이어도 누적 허용)
        if (storage.TryGetValue(itemName, out int current))
        {
            long after = (long)current + amount;
            return after <= maxStackPerItem;   // 100 이상이면 false
        }

        // 새 아이템이면: 반드시 빈 슬롯 필요
        return FreeSlots >= 1 && amount <= maxStackPerItem;
    }

    public bool TryAddItem(string itemName, int amount)
    {
        if (!HasRoomFor(itemName, amount)) return false;
        AddItem(itemName, amount); // 기존 AddItem 재사용(음수 방어 등)
        return true;
    }
}

