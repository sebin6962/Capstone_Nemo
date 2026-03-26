using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerLevelManager : MonoBehaviour
{
    public static PlayerLevelManager Instance;

    public int Level { get; private set; } = 1;
    public int Exp { get; private set; } = 0;
    public int ExpToNextLevel => (100 + (Level - 1) * 50) *2;

    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // savePath는 서버명 할당될 때 지정!
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetServerName(string serverName)
    {
        savePath = Path.Combine(Application.persistentDataPath, $"player_level_data_{serverName}.json");
    }

    public void AddExp(int amount)
    {
        Exp += amount;
        while (Exp >= ExpToNextLevel)
        {
            Exp -= ExpToNextLevel;
            Level++;
            Debug.Log($"레벨업! 현재 레벨: {Level}");
            UnlockManager.Instance?.ScheduleUnlockForLevel(Level); //레벨업 시 다음 날 적용
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
        File.WriteAllText(savePath, json);
    }

    public void Load()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            PlayerLevelData data = JsonUtility.FromJson<PlayerLevelData>(json);
            if (data != null)
            {
                this.Level = data.Level;
                this.Exp = data.Exp;
            }
        }
        else
        {
            Level = 1;
            Exp = 0;
            Save();
        }
    }

    public void SetLevelAndExp(int level, int exp)
    {
        this.Level = Mathf.Max(1, level);
        this.Exp = Mathf.Max(0, exp);
    }
}

[System.Serializable]
public class PlayerLevelData
{
    public int Level;
    public int Exp;
}
