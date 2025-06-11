using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolData : MonoBehaviour
{
    public static ToolData Instance;

    public List<Tool> toolList;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadToolData();
    }

    private void LoadToolData()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("toolData");

        if (jsonText != null)
        {
            toolList = JsonUtility.FromJson<ToolList>("{\"tools\":" + jsonText.text + "}").tools;
            Debug.Log("도구 데이터 로드 완료");
        }
        else
        {
            Debug.LogWarning("toolData.json을 찾을 수 없습니다.");
            toolList = new List<Tool>();
        }
    }

    public bool IsTool(string name)
    {
        return toolList.Exists(tool => tool.name == name);
    }

    public Tool GetToolByName(string name)
    {
        return toolList.Find(tool => tool.name == name);
    }

    public Tool GetToolById(string id)
    {
        return toolList.Find(tool => tool.id == id);
    }

    public bool IsWateringCan(GameObject obj)
    {
        return obj.name.Contains("wateringCan") || obj.CompareTag("WateringCan");
    }
}