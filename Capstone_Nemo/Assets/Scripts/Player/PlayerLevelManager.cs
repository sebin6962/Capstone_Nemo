using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerLevelManager : MonoBehaviour
{
    public static PlayerLevelManager Instance;

    public int Level { get; private set; } = 1;
    public int Exp { get; private set; } = 0;
    public int ExpToNextLevel => 100 + (Level - 1) * 50;

    private string SavePath => Path.Combine(Application.persistentDataPath, "player_level_data.json");

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddExp(int amount)
    {
        Exp += amount;
        while (Exp >= ExpToNextLevel)
        {
            Exp -= ExpToNextLevel;
            Level++;
            Debug.Log($"레벨업! 현재 레벨: {Level}");
        }
        Debug.Log($"경험치 획득: {amount}, 현재 Exp: {Exp}/{ExpToNextLevel}");
        Save(); // 경험치가 바뀔 때마다 저장
    }

    public void Save()
    {
        PlayerLevelData data = new PlayerLevelData
        {
            Level = this.Level,
            Exp = this.Exp
        };
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[PlayerLevelManager] 저장됨: {SavePath}");
    }

    public void Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            PlayerLevelData data = JsonUtility.FromJson<PlayerLevelData>(json);
            if (data != null)
            {
                this.Level = data.Level;
                this.Exp = data.Exp;
            }
            Debug.Log($"[PlayerLevelManager] 불러오기 완료: Lv.{Level} / Exp {Exp}");
        }
        else
        {
            Debug.Log("[PlayerLevelManager] 저장된 레벨 데이터 없음. 새로 생성.");
            Level = 1;
            Exp = 0;
        }
    }
}

[System.Serializable]
public class PlayerLevelData
{
    public int Level;
    public int Exp;
}
