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

            // 서버 선택값으로 초기화 시도
            InitFromSelectedSave();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// SaveSelect/NewGame에서 이미 SelectedSave를 세팅함.
    /// 씬 진입 시 여기서 경로/로드를 보장.
    /// </summary>
    public void InitFromSelectedSave()
    {
        // 서버명 없으면 기본값(임시)로 동작하지 않고, 0으로 메모리만 유지
        var serverName = PlayerPrefs.GetString("SelectedSave", string.Empty);
        if (!string.IsNullOrEmpty(serverName))
        {
            SetServerName(serverName);
            LoadStarData();
        }
        else
        {
            // 경로 미설정. 필요 시 나중에 SetServerName() 호출 후 LoadStarData().
            playerData.starlight = 0;
        }
    }

    public void SetServerName(string serverName)
    {
        savePath = Application.persistentDataPath + $"/playerStarData_{serverName}.json";
    }

    public void SaveStarData()
    {
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("[StarDataManager] savePath is null/empty. Call SetServerName() first.");
            return;
        }
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadStarData()
    {
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("[StarDataManager] savePath is null/empty. Call SetServerName() first.");
            return;
        }

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
        var ui = FindObjectOfType<StarlightUI>();
        if (ui != null) ui.UpdateStarlightUI();
    }

    public void SpendStarlight(int amount)
    {
        playerData.starlight -= amount;
        SaveStarData();
        var ui = FindObjectOfType<StarlightUI>();
        if (ui != null) ui.UpdateStarlightUI();
    }

    public void SetStarlight(int starlight)
    {
        playerData.starlight = starlight;
        SaveStarData();
        var ui = FindObjectOfType<StarlightUI>();
        if (ui != null) ui.UpdateStarlightUI();
    }
}

