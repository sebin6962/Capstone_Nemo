using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class PlayerData
{
    public int starlight;
    public DayReport todayReport;
    public DayReport yesterdayReport;
}

[System.Serializable]
public struct DayReport
{
    public int normalCount;
    public int normalStars;

    // 기존 특별 손님 데이터는 총 별빛 계산을 위해 유지
    public int questCount;
    public int questStars;

    // 해당 날짜에 판매한 다과 종류
    public List<string> soldDagwaKeys;

    public int TotalStars => normalStars + questStars;
}

public class StarDataManager : MonoBehaviour
{
    public static StarDataManager Instance;

    public PlayerData playerData = new PlayerData();

    private string savePath;

    private DayReport _today;
    private DayReport _yesterday;

    private static DayReport CreateEmptyReport()
    {
        return new DayReport
        {
            normalCount = 0,
            normalStars = 0,
            questCount = 0,
            questStars = 0,
            soldDagwaKeys = new List<string>()
        };
    }

    private static DayReport NormalizeReport(DayReport report)
    {
        // 기존 세이브 파일에는 soldDagwaKeys가 없기 때문에
        // 불러왔을 때 null이 될 수 있음
        if (report.soldDagwaKeys == null)
        {
            report.soldDagwaKeys = new List<string>();
        }

        return report;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitFromSelectedSave();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        TimeManager.OnNewDayStarted += SnapshotAndReset;
    }

    private void OnDisable()
    {
        TimeManager.OnNewDayStarted -= SnapshotAndReset;
    }

    /// <summary>
    /// 다음 날이 시작될 때 오늘 데이터를 어제 데이터로 넘긴다.
    /// </summary>
    private void SnapshotAndReset()
    {
        _yesterday = NormalizeReport(_today);
        _today = CreateEmptyReport();

        SaveStarData();
    }

    /// <summary>
    /// 명세서에서 표시할 어제 판매 결과를 반환한다.
    /// </summary>
    public DayReport GetYesterdayReport()
    {
        return NormalizeReport(_yesterday);
    }

    /// <summary>
    /// 판매 완료된 다과 종류를 오늘 판매 목록에 기록한다.
    /// 같은 다과를 여러 개 판매해도 한 번만 기록된다.
    /// </summary>
    public void RecordSoldDagwa(string dagwaKey)
    {
        if (string.IsNullOrWhiteSpace(dagwaKey))
        {
            return;
        }

        dagwaKey = dagwaKey.Trim();
        _today = NormalizeReport(_today);

        bool alreadyRecorded = _today.soldDagwaKeys.Exists(
            savedKey => string.Equals(
                savedKey,
                dagwaKey,
                System.StringComparison.OrdinalIgnoreCase
            )
        );

        if (alreadyRecorded)
        {
            return;
        }

        _today.soldDagwaKeys.Add(dagwaKey);

        SaveStarData();
    }

    /// <summary>
    /// 일반 손님 판매 실적과 별빛을 추가한다.
    /// </summary>
    public void AddStarlightFromNormal(int amount)
    {
        _today = NormalizeReport(_today);

        _today.normalCount++;
        _today.normalStars += amount;

        AddStarlight(amount);
    }

    /// <summary>
    /// 특별 손님 판매 실적과 별빛을 추가한다.
    /// 특별 손님 행은 명세서에 표시하지 않지만
    /// 총 별빛 계산을 위해 데이터는 계속 유지한다.
    /// </summary>
    public void AddStarlightFromQuest(int amount)
    {
        _today = NormalizeReport(_today);

        _today.questCount++;
        _today.questStars += amount;

        AddStarlight(amount);
    }

    /// <summary>
    /// 현재 선택된 세이브에 맞춰 저장 경로를 설정하고 데이터를 불러온다.
    /// </summary>
    public void InitFromSelectedSave()
    {
        string serverName = PlayerPrefs.GetString(
            "SelectedSave",
            string.Empty
        );

        if (!string.IsNullOrEmpty(serverName))
        {
            SetServerName(serverName);
            LoadStarData();
        }
        else
        {
            playerData.starlight = 0;

            _today = CreateEmptyReport();
            _yesterday = CreateEmptyReport();
        }
    }

    public void SetServerName(string serverName)
    {
        savePath =
            Application.persistentDataPath +
            $"/playerStarData_{serverName}.json";
    }

    public void SaveStarData()
    {
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError(
                "[StarDataManager] savePath가 설정되지 않았습니다. " +
                "SetServerName()을 먼저 호출해야 합니다."
            );

            return;
        }

        playerData.todayReport = NormalizeReport(_today);
        playerData.yesterdayReport = NormalizeReport(_yesterday);

        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadStarData()
    {
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError(
                "[StarDataManager] savePath가 설정되지 않았습니다. " +
                "SetServerName()을 먼저 호출해야 합니다."
            );

            return;
        }

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);

            playerData = JsonUtility.FromJson<PlayerData>(json);

            if (playerData == null)
            {
                playerData = new PlayerData();
            }

            // 기존 세이브 데이터 호환 처리
            _today = NormalizeReport(playerData.todayReport);
            _yesterday = NormalizeReport(playerData.yesterdayReport);
        }
        else
        {
            playerData = new PlayerData();
            playerData.starlight = 0;

            _today = CreateEmptyReport();
            _yesterday = CreateEmptyReport();

            SaveStarData();
        }
    }

    public void AddStarlight(int amount)
    {
        playerData.starlight += amount;

        SaveStarData();

        StarlightUI ui = FindObjectOfType<StarlightUI>();

        if (ui != null)
        {
            ui.UpdateStarlightUI();
        }
    }

    public void SpendStarlight(int amount)
    {
        playerData.starlight -= amount;

        SaveStarData();

        StarlightUI ui = FindObjectOfType<StarlightUI>();

        if (ui != null)
        {
            ui.UpdateStarlightUI();
        }
    }

    public void SetStarlight(int starlight)
    {
        playerData.starlight = starlight;

        SaveStarData();

        StarlightUI ui = FindObjectOfType<StarlightUI>();

        if (ui != null)
        {
            ui.UpdateStarlightUI();
        }
    }
}