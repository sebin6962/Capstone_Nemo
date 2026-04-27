using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DailyQuestManager : MonoBehaviour
{
    public static DailyQuestManager Instance;

    [SerializeField] private int dailyQuestCount = 3;

    private readonly List<QuestData> todayQuests = new();

    private string currentLoadedSave = "";
    private bool loadedThisSession = false;

    public IReadOnlyList<QuestData> TodayQuests => todayQuests;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        EnsureTodayQuestsLoadedOncePerSession();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureTodayQuestsLoadedOncePerSession();
    }

    public void EnsureTodayQuestsLoadedOncePerSession()
    {
        string serverName = PlayerPrefs.GetString("SelectedSave", "");
        if (string.IsNullOrEmpty(serverName))
            return;

        if (currentLoadedSave != serverName)
        {
            currentLoadedSave = serverName;
            loadedThisSession = false;
            todayQuests.Clear();
        }

        if (loadedThisSession)
            return;

        LoadOrGenerateTodayQuests(serverName);
        loadedThisSession = true;
    }

    public void SwitchToSave(string serverName)
    {
        if (string.IsNullOrEmpty(serverName))
            return;

        currentLoadedSave = serverName;
        loadedThisSession = false;
        todayQuests.Clear();

        LoadOrGenerateTodayQuests(serverName);
        loadedThisSession = true;
    }

    private void LoadOrGenerateTodayQuests(string serverName)
    {
        if (QuestDatabase.Instance == null || QuestDatabase.Instance.QuestList == null)
        {
            Debug.LogWarning("[DailyQuestManager] QuestDatabase가 아직 준비되지 않았습니다.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, $"save_myuser_{serverName}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[DailyQuestManager] 세이브 파일이 없습니다: {path}");
            return;
        }

        SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (saveData == null)
        {
            Debug.LogWarning("[DailyQuestManager] SaveData 로드 실패");
            return;
        }

        if (saveData.dailyQuestIds == null)
            saveData.dailyQuestIds = new List<string>();

        string today = DateTime.Now.ToString("yyyy-MM-dd");

        // 오늘 처음 접속한 경우에만 새로 생성
        if (saveData.dailyQuestRealDate != today || saveData.dailyQuestIds.Count == 0)
        {
            GenerateDailyQuests(saveData);
            saveData.dailyQuestRealDate = today;
            SaveDailyQuestData(saveData, serverName);
        }
        else
        {
            LoadDailyQuestsFromSaveData(saveData);
        }

        Debug.Log($"[DailyQuestManager] 세이브 [{serverName}] 오늘의 퀘스트 {todayQuests.Count}개 준비 완료 / 기준 날짜: {saveData.dailyQuestRealDate}");
    }

    private void GenerateDailyQuests(SaveData saveData)
    {
        todayQuests.Clear();

        List<QuestData> pool = new List<QuestData>(QuestDatabase.Instance.QuestList);
        Shuffle(pool);

        int pickCount = Mathf.Min(dailyQuestCount, pool.Count);

        saveData.dailyQuestIds = new List<string>();

        for (int i = 0; i < pickCount; i++)
        {
            QuestData quest = pool[i];
            if (quest == null || string.IsNullOrEmpty(quest.id))
                continue;

            todayQuests.Add(quest);
            saveData.dailyQuestIds.Add(quest.id);
        }
    }

    private void LoadDailyQuestsFromSaveData(SaveData saveData)
    {
        todayQuests.Clear();

        if (saveData.dailyQuestIds == null)
            return;

        for (int i = 0; i < saveData.dailyQuestIds.Count; i++)
        {
            string questId = saveData.dailyQuestIds[i];
            if (string.IsNullOrEmpty(questId))
                continue;

            QuestData quest = QuestDatabase.Instance.GetQuestById(questId);
            if (quest != null)
                todayQuests.Add(quest);
        }
    }

    private void SaveDailyQuestData(SaveData saveData, string serverName)
    {
        string path = Path.Combine(Application.persistentDataPath, $"save_myuser_{serverName}.json");
        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
    }

    private void Shuffle(List<QuestData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}