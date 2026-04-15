using System.Collections.Generic;
using UnityEngine;

public class NPCDialogueDatabase : MonoBehaviour
{
    public static NPCDialogueDatabase Instance;

    [SerializeField] private string jsonFileName = "NPCDialogueData";

    private Dictionary<string, NPCDialogueData> dialogueDict = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadDialogueData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadDialogueData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);

        if (jsonFile == null)
        {
            Debug.LogError($"[NPCDialogueDatabase] Resources/{jsonFileName}.json 파일을 찾을 수 없습니다.");
            return;
        }

        NPCDialogueDataList dataList = JsonUtility.FromJson<NPCDialogueDataList>(jsonFile.text);

        if (dataList == null || dataList.npcs == null)
        {
            Debug.LogError("[NPCDialogueDatabase] NPC 대화 JSON 파싱 실패");
            return;
        }

        dialogueDict.Clear();

        foreach (var npc in dataList.npcs)
        {
            if (npc == null || string.IsNullOrEmpty(npc.npcId))
                continue;

            dialogueDict[npc.npcId] = npc;
        }

        Debug.Log($"[NPCDialogueDatabase] NPC 대화 {dialogueDict.Count}개 로드 완료");
    }

    public NPCDialogueData GetDialogueByNpcId(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;

        dialogueDict.TryGetValue(npcId, out var data);
        return data;
    }
}
