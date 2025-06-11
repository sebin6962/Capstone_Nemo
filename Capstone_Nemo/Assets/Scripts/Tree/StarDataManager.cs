using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class PlayerData
{
    public int starlight;
}

public class StarDataManager : MonoBehaviour
{
    public static StarDataManager Instance;
    public PlayerData playerData = new PlayerData();

    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Application.persistentDataPath + "/playerStarData.json";
            LoadStarData();
            SaveStarData(); //�׽�Ʈ�� ���� �� �ٷ� json���� ����
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveStarData()
    {
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadStarData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            playerData = JsonUtility.FromJson<PlayerData>(json);
        }
        else
        {
            playerData.starlight = 0;
            SaveStarData();
        }
    }

    public void AddStarlight(int amount)
    {
        playerData.starlight += amount;
        SaveStarData();
        FindObjectOfType<StarlightUI>().UpdateStarlightUI();
    }

    public void SpendStarlight(int amount)
    {
        playerData.starlight -= amount;
        SaveStarData();
        FindObjectOfType<StarlightUI>().UpdateStarlightUI();
    }
}
