using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class MakerPopupEntry
{
    public string tag;
    public string makerId;
    public string category;
    public string title;
    public string actionLabel;
}

[System.Serializable]
public class MakerPopupEntryList
{
    public List<MakerPopupEntry> makers;
}

public static class MakerPopupData
{
    public static Dictionary<string, MakerPopupEntry> popupMap = new();

    static MakerPopupData()
    {
        LoadConfig();
    }

    private static void LoadConfig()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/MakerPopupConfig");
        if (json == null)
        {
            Debug.LogError("[MakerPopupData] MakerPopupConfig.json 파일을 찾을 수 없습니다.");
            return;
        }

        MakerPopupEntryList list = JsonUtility.FromJson<MakerPopupEntryList>(json.text);
        popupMap.Clear();

        foreach (var entry in list.makers)
        {
            popupMap[entry.makerId] = entry;
        }

        Debug.Log($"[MakerPopupData] 제작기 팝업 매핑 {popupMap.Count}개 로드됨");
    }
}
