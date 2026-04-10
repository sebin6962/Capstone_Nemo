using System.Collections.Generic;
using UnityEngine;

public class QuestDatabase : MonoBehaviour
{
    public static QuestDatabase Instance;

    [SerializeField] private string jsonFileName = "QuestData";

    private List<QuestData> questList = new List<QuestData>();

    public List<QuestData> QuestList => questList;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadQuestData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadQuestData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);

        if (jsonFile == null)
        {
            Debug.LogError($"[QuestDatabase] Resources/Data/{jsonFileName}.json 파일을 찾을 수 없습니다.");
            return;
        }

        QuestDataList dataList = JsonUtility.FromJson<QuestDataList>(jsonFile.text);

        if (dataList == null || dataList.quests == null)
        {
            Debug.LogError("[QuestDatabase] 퀘스트 JSON 파싱 실패");
            return;
        }

        questList = dataList.quests;
        Debug.Log($"[QuestDatabase] 퀘스트 {questList.Count}개 로드 완료");
    }

    public QuestData GetQuestById(string questId)
    {
        return questList.Find(q => q.id == questId);
    }
}
