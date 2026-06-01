using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class TableSlotSave
{
    public string tableId;
    public string itemSpriteName;
}

[Serializable]
public class TableSaveData
{
    public List<TableSlotSave> tables = new List<TableSlotSave>();
}

public class TableManager : MonoBehaviour
{
    string CurrentServer => PlayerPrefs.GetString("SelectedSave", "");

    string TableSavePath
        => string.IsNullOrEmpty(CurrentServer)
           ? null
           : Path.Combine(Application.persistentDataPath, $"ps_tableItem_{CurrentServer}.json");

    void Start()
    {
        bool hasSave =
        !string.IsNullOrEmpty(TableSavePath) &&
        File.Exists(TableSavePath);

        if (hasSave)
        {
            LoadTableState(); 
        }

        SpawnInitialItemsUnique();
    }

    void OnDisable()
    {
        SaveTableState();
    }

    void OnApplicationQuit()
    {
        SaveTableState();
    }

    private void SpawnInitialItemsUnique()
    {
        var tablesInScene = FindObjectsOfType<TableInfo>();

        var existingSpriteNames = new HashSet<string>();
        foreach (var t in tablesInScene)
        {
            if (t.currentPlacedObject == null) continue;

            var sr = t.currentPlacedObject.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                existingSpriteNames.Add(sr.sprite.name);
            }
        }

        foreach (var t in tablesInScene)
        {
            if (!t.spawnInitialItemOnStart) continue;
            if (string.IsNullOrEmpty(t.initialItemSpriteName)) continue;

            if (existingSpriteNames.Contains(t.initialItemSpriteName))
                continue;

            if (t.currentPlacedObject != null)
                continue;

            if (t.TrySpawnInitialItem())
            {
                existingSpriteNames.Add(t.initialItemSpriteName);
            }
        }
    }
    public void SaveTableState()
    {
        if (string.IsNullOrEmpty(TableSavePath)) return;

        var tablesInScene = FindObjectsOfType<TableInfo>();
        if (tablesInScene.Length == 0)
        {
            Debug.Log("[Table] 씬에 TableInfo 없음 → 기존 table json 덮어쓰기 생략");
            return;
        }

        var data = new TableSaveData();

        foreach (var table in tablesInScene)
        {
            if (string.IsNullOrEmpty(table.tableId)) continue;

            // 테이블 위에 아무것도 없으면 저장 안 함 (이 테이블은 비어있는 상태로 간주)
            if (table.currentPlacedObject == null) continue;

            var sr = table.currentPlacedObject.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            // 초기 아이템이면 세이브에서 제외
            if (table.spawnInitialItemOnStart &&
                !string.IsNullOrEmpty(table.initialItemSpriteName) &&
                sr.sprite.name == table.initialItemSpriteName)
            {
                // JSON에 안 넣음
                continue;
            }

            var slot = new TableSlotSave
            {
                tableId = table.tableId,
                itemSpriteName = sr.sprite.name
            };

            data.tables.Add(slot);
        }

        File.WriteAllText(TableSavePath, JsonUtility.ToJson(data, true));
        Debug.Log($"[Table] Saved {data.tables.Count} tables → {TableSavePath}");
    }

    public void LoadTableState()
    {
        if (string.IsNullOrEmpty(TableSavePath)) return;
        if (!File.Exists(TableSavePath)) return;

        var json = File.ReadAllText(TableSavePath);
        var data = JsonUtility.FromJson<TableSaveData>(json);
        if (data == null) return;

        var tablesInScene = FindObjectsOfType<TableInfo>();
        var map = new Dictionary<string, TableInfo>();

        foreach (var t in tablesInScene)
        {
            if (!string.IsNullOrEmpty(t.tableId))
                map[t.tableId] = t;
        }

        foreach (var saved in data.tables)
        {
            if (!map.TryGetValue(saved.tableId, out var table))
                continue;

            // 기존에 올라가 있던 초기 아이템/이전 상태 제거
            if (table.currentPlacedObject != null)
            {
                Destroy(table.currentPlacedObject);
                table.currentPlacedObject = null;
            }

            if (string.IsNullOrEmpty(saved.itemSpriteName))
                continue;

            // 스프라이트 로드
            Sprite spr = Resources.Load<Sprite>(table.spriteResourceDir + saved.itemSpriteName);
            if (spr == null)
            {
                Debug.LogWarning($"[Table] 스프라이트 로드 실패: {table.spriteResourceDir}{saved.itemSpriteName}");
                continue;
            }

            // 테이블 위에 새 TableItem 생성
            table.CreateTableItem(spr);

            Debug.Log($"[Table] 복원: tableId={saved.tableId}, item={saved.itemSpriteName}");
        }

        Debug.Log($"[Table] Loaded {data.tables.Count} tables");
    }
}

public static class TableInitialItemHelper
{
    // 씬에 있는 TableInfo들을 보고 "초기 아이템 스프라이트 이름" 목록을 만든다.
    public static bool IsInitialTableItemName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return false;

        var tables = GameObject.FindObjectsOfType<TableInfo>();
        foreach (var t in tables)
        {
            if (!t.spawnInitialItemOnStart) continue;
            if (string.IsNullOrEmpty(t.initialItemSpriteName)) continue;

            if (t.initialItemSpriteName == spriteName)
                return true;
        }

        return false;
    }
}
