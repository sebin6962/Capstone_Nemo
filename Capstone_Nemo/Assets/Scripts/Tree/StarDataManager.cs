using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class PlayerData
{
    public int starlight;
}

[System.Serializable]
public struct DayReport
{
    public int normalCount, normalStars;
    public int questCount, questStars;
    public int TotalStars => normalStars + questStars;
}

public class StarDataManager : MonoBehaviour
{
    public static StarDataManager Instance;
    public PlayerData playerData = new PlayerData();

    private string savePath;

    private DayReport _today, _yesterday;

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

    // 하루 끝(다음 날 시작 이벤트)에 스냅샷 전환
    void OnEnable()
    {
        TimeManager.OnNewDayStarted += SnapshotAndReset; // TimeManager 이벤트가 이미 존재한다고 가정
    }
    void OnDisable()
    {
        TimeManager.OnNewDayStarted -= SnapshotAndReset;
    }

    private void SnapshotAndReset()
    {
        _yesterday = _today;        // 어제 성과로 스냅샷
        _today = new DayReport();   // 금일 집계 초기화
    }

    // 외부에서 읽기 위한 getter
    public DayReport GetYesterdayReport() => _yesterday;

    // 집계 + 총 별빛 반영 (정규 손님)
    public void AddStarlightFromNormal(int amount)
    {
        _today.normalCount++;
        _today.normalStars += amount;
        AddStarlight(amount); // 기존 총 별빛 저장/UI 갱신 로직 그대로 사용
    }

    // 집계 + 총 별빛 반영 (특별 손님)
    public void AddStarlightFromQuest(int amount)
    {
        _today.questCount++;
        _today.questStars += amount;
        AddStarlight(amount);
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

