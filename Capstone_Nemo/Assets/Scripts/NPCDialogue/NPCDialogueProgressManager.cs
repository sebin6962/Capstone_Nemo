using System.Collections.Generic;
using UnityEngine;

public class NPCDialogueProgressManager : MonoBehaviour
{
    public static NPCDialogueProgressManager Instance;

    private const string SaveKey = "NPC_DIALOGUE_PROGRESS";

    [SerializeField] private NPCDialogueProgressDataList progressData = new NPCDialogueProgressDataList();

    private void Awake()
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

    public NPCDialogueNpcProgressData GetOrCreateNpcProgress(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
            return null;

        if (progressData == null)
            progressData = new NPCDialogueProgressDataList();

        if (progressData.npcProgressList == null)
            progressData.npcProgressList = new List<NPCDialogueNpcProgressData>();

        NPCDialogueNpcProgressData npcProgress =
            progressData.npcProgressList.Find(p => p.npcId == npcId);

        if (npcProgress == null)
        {
            npcProgress = new NPCDialogueNpcProgressData();
            npcProgress.npcId = npcId;
            progressData.npcProgressList.Add(npcProgress);
        }

        if (npcProgress.categoryProgressList == null)
            npcProgress.categoryProgressList = new List<NPCDialogueCategoryProgressData>();

        return npcProgress;
    }

    public void Save()
    {
        if (progressData == null)
            progressData = new NPCDialogueProgressDataList();

        string json = JsonUtility.ToJson(progressData);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            progressData = new NPCDialogueProgressDataList();
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json))
        {
            progressData = new NPCDialogueProgressDataList();
            return;
        }

        progressData = JsonUtility.FromJson<NPCDialogueProgressDataList>(json);

        if (progressData == null)
            progressData = new NPCDialogueProgressDataList();

        if (progressData.npcProgressList == null)
            progressData.npcProgressList = new List<NPCDialogueNpcProgressData>();
    }

    public void ResetAllProgress()
    {
        progressData = new NPCDialogueProgressDataList();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
