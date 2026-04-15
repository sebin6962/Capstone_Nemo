using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class QuestAcceptManager : MonoBehaviour
{
    public static QuestAcceptManager Instance;

    [SerializeField] private int maxAcceptedQuestCount = 3;

    private readonly List<QuestData> acceptedQuests = new();
    private string currentLoadedSave = "";
    private bool hasInitialized = false;

    public IReadOnlyList<QuestData> AcceptedQuests => acceptedQuests;
    public int MaxAcceptedQuestCount => maxAcceptedQuestCount;

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
        EnsureLoadedForCurrentSave();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureLoadedForCurrentSave();
        RefreshHUD();
    }

    public void EnsureLoadedForCurrentSave()
    {
        string selectedSave = PlayerPrefs.GetString("SelectedSave", "");

        if (!hasInitialized)
        {
            LoadFromSave(selectedSave);
            hasInitialized = true;
            return;
        }

        if (currentLoadedSave != selectedSave)
        {
            LoadFromSave(selectedSave);
        }
    }

    public void SwitchToSave(string saveName)
    {
        PlayerPrefs.SetString("SelectedSave", saveName);
        PlayerPrefs.Save();

        LoadFromSave(saveName);
        RefreshHUD();
    }

    public bool IsAccepted(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return false;

        for (int i = 0; i < acceptedQuests.Count; i++)
        {
            if (acceptedQuests[i] != null && acceptedQuests[i].id == questId)
                return true;
        }

        return false;
    }

    public bool CanAcceptMore()
    {
        return acceptedQuests.Count < maxAcceptedQuestCount;
    }

    public bool TryAcceptQuest(QuestData questData)
    {
        if (questData == null) return false;
        if (string.IsNullOrEmpty(questData.id)) return false;

        EnsureLoadedForCurrentSave();

        if (IsAccepted(questData.id))
            return true;

        if (!CanAcceptMore())
            return false;

        acceptedQuests.Add(questData);
        SaveToCurrentSaveFile();
        RefreshHUD();
        return true;
    }

    public void RemoveAcceptedQuest(string questId)
    {
        EnsureLoadedForCurrentSave();

        for (int i = acceptedQuests.Count - 1; i >= 0; i--)
        {
            if (acceptedQuests[i] != null && acceptedQuests[i].id == questId)
            {
                acceptedQuests.RemoveAt(i);
                break;
            }
        }

        SaveToCurrentSaveFile();
        RefreshHUD();
    }

    private void LoadFromSave(string saveName)
    {
        acceptedQuests.Clear();
        currentLoadedSave = saveName;

        if (string.IsNullOrEmpty(saveName))
        {
            Debug.LogWarning("[QuestAcceptManager] 선택된 세이브가 없습니다.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, $"save_myuser_{saveName}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[QuestAcceptManager] 세이브 파일이 없습니다: {path}");
            return;
        }

        SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (saveData == null)
        {
            Debug.LogWarning("[QuestAcceptManager] SaveData 로드 실패");
            return;
        }

        if (saveData.acceptedQuestIds == null)
            saveData.acceptedQuestIds = new List<string>();

        if (QuestDatabase.Instance == null)
        {
            Debug.LogWarning("[QuestAcceptManager] QuestDatabase.Instance가 없습니다.");
            return;
        }

        foreach (string questId in saveData.acceptedQuestIds)
        {
            if (string.IsNullOrEmpty(questId)) continue;

            QuestData questData = QuestDatabase.Instance.GetQuestById(questId);
            if (questData != null)
                acceptedQuests.Add(questData);
        }

        Debug.Log($"[QuestAcceptManager] 세이브 [{saveName}] 퀘스트 {acceptedQuests.Count}개 로드 완료");
    }

    public void LoadFromCurrentSaveFile()
    {
        string selectedSave = PlayerPrefs.GetString("SelectedSave", "");
        LoadFromSave(selectedSave);
    }

    public void SaveToCurrentSaveFile()
    {
        string serverName = PlayerPrefs.GetString("SelectedSave", "");
        if (string.IsNullOrEmpty(serverName))
        {
            Debug.LogWarning("[QuestAcceptManager] SelectedSave가 비어 있습니다.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, $"save_myuser_{serverName}.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[QuestAcceptManager] 세이브 파일이 없습니다: {path}");
            return;
        }

        SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (saveData == null)
            saveData = new SaveData();

        saveData.acceptedQuestIds = new List<string>();

        for (int i = 0; i < acceptedQuests.Count; i++)
        {
            if (acceptedQuests[i] != null && !string.IsNullOrEmpty(acceptedQuests[i].id))
                saveData.acceptedQuestIds.Add(acceptedQuests[i].id);
        }

        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
        currentLoadedSave = serverName;

        Debug.Log($"[QuestAcceptManager] 세이브 [{serverName}] 퀘스트 저장 완료: {saveData.acceptedQuestIds.Count}개");
    }

    public QuestData GetAcceptedTalkQuestForNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;

        for (int i = 0; i < acceptedQuests.Count; i++)
        {
            QuestData quest = acceptedQuests[i];
            if (quest == null) continue;

            if (quest.targetType == "Talk" && quest.targetId == npcId)
                return quest;
        }

        return null;
    }

    public void CompleteAcceptedTalkQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;

        for (int i = acceptedQuests.Count - 1; i >= 0; i--)
        {
            if (acceptedQuests[i] != null && acceptedQuests[i].id == questId)
            {
                acceptedQuests.RemoveAt(i);
                break;
            }
        }

        SaveToCurrentSaveFile();
        RefreshHUD();
    }
    private void RefreshHUD()
    {
        if (QuestHUDUIManager.Instance != null)
            QuestHUDUIManager.Instance.RefreshAcceptedQuestUI();
    }
}