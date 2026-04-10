using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class QuestAcceptManager : MonoBehaviour
{
    public static QuestAcceptManager Instance;

    [SerializeField] private int maxAcceptedQuestCount = 3;

    private readonly List<QuestData> acceptedQuests = new();
    private bool hasLoaded = false;

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
        if (!hasLoaded)
        {
            LoadFromCurrentSaveFile();
            hasLoaded = true;
        }

        RefreshHUD();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (QuestHUDUIManager.Instance != null)
            QuestHUDUIManager.Instance.RefreshAcceptedQuestUI();
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

    public void LoadFromCurrentSaveFile()
    {
        acceptedQuests.Clear();

        string serverName = PlayerPrefs.GetString("SelectedSave", "");
        if (string.IsNullOrEmpty(serverName)) return;

        string path = Path.Combine(Application.persistentDataPath, $"save_myuser_{serverName}.json");
        if (!File.Exists(path)) return;

        SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (saveData == null) return;

        if (saveData.acceptedQuestIds == null)
            saveData.acceptedQuestIds = new List<string>();

        if (QuestDatabase.Instance == null) return;

        foreach (string questId in saveData.acceptedQuestIds)
        {
            if (string.IsNullOrEmpty(questId)) continue;

            QuestData questData = QuestDatabase.Instance.GetQuestById(questId);
            if (questData != null)
                acceptedQuests.Add(questData);
        }
    }

    public void SaveToCurrentSaveFile()
    {
        string serverName = PlayerPrefs.GetString("SelectedSave", "");
        if (string.IsNullOrEmpty(serverName)) return;

        string path = Path.Combine(Application.persistentDataPath, $"save_myuser_{serverName}.json");
        if (!File.Exists(path)) return;

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
    }
}
