using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도감의 '기록' 탭에서 사용하는 영구 발견 기록.
/// NPCDialogueProgressManager의 seenSetIds는 대화 순환을 위해 초기화될 수 있으므로
/// 도감 수집 기록은 별도로 보존한다.
/// 저장 슬롯별 PlayerPrefs 키를 사용한다.
/// </summary>
public static class DoGamRecordProgress
{
    [Serializable]
    private class StringListData
    {
        public List<string> values = new List<string>();
    }

    private const string DialogueKeyBase = "DoGam_Record_DiscoveredDialogues";
    private const string UnreadDialogueKeyBase = "DoGam_Record_UnreadDialogues";
    private const string WorldRecordKeyBase = "DoGam_Record_DiscoveredWorldRecords";

    private static string cachedSlot;
    private static HashSet<string> dialogueIds;
    private static HashSet<string> unreadDialogueIds;
    private static HashSet<string> worldRecordIds;

    public static event Action ProgressChanged;
    public static event Action UnreadChanged;

    public static bool IsDialogueDiscovered(string npcId, string setId)
    {
        EnsureLoaded();
        return dialogueIds.Contains(MakeDialogueId(npcId, setId));
    }

    public static void MarkDialogueDiscovered(string npcId, string setId)
    {
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(setId))
            return;

        EnsureLoaded();

        string dialogueId = MakeDialogueId(npcId, setId);

        if (!dialogueIds.Add(dialogueId))
            return;

        unreadDialogueIds.Add(dialogueId);

        Save(DialogueKeyBase, dialogueIds);
        Save(UnreadDialogueKeyBase, unreadDialogueIds);
        ProgressChanged?.Invoke();
        UnreadChanged?.Invoke();
    }

    public static bool IsDialogueUnread(string npcId, string setId)
    {
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(setId))
            return false;

        EnsureLoaded();
        return unreadDialogueIds.Contains(MakeDialogueId(npcId, setId));
    }

    public static bool HasUnreadDialogues()
    {
        EnsureLoaded();
        return unreadDialogueIds.Count > 0;
    }

    public static void MarkDialogueRead(string npcId, string setId)
    {
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(setId))
            return;

        EnsureLoaded();

        if (!unreadDialogueIds.Remove(MakeDialogueId(npcId, setId)))
            return;

        Save(UnreadDialogueKeyBase, unreadDialogueIds);
        UnreadChanged?.Invoke();
    }

    /// <summary>
    /// NPCDialogueUIManager에서 선택된 시작 노드만 알고 있을 때 호출 가능.
    /// 해당 startNodeId를 가진 dialogueSet을 찾아 영구 발견 처리한다.
    /// </summary>
    public static void MarkDialogueFromStartNode(NPCDialogueData npcData, string startNodeId)
    {
        if (npcData == null ||
            npcData.dialogueSets == null ||
            string.IsNullOrWhiteSpace(startNodeId))
            return;

        NPCDialogueSetData set =
            npcData.dialogueSets.Find(x =>
                x != null &&
                string.Equals(x.startNodeId, startNodeId, StringComparison.Ordinal));

        if (set != null)
            MarkDialogueDiscovered(npcData.npcId, set.setId);
    }

    /// <summary>
    /// 기존 NPCDialogueProgressManager에 남아 있는 seenSetIds를
    /// 도감 영구 기록으로 1회 흡수한다.
    /// </summary>
    public static void ImportNpcDialogueProgress(
        string npcId,
        NPCDialogueNpcProgressData progress)
    {
        if (string.IsNullOrWhiteSpace(npcId) ||
            progress == null ||
            progress.categoryProgressList == null)
            return;

        EnsureLoaded();
        bool changed = false;

        foreach (NPCDialogueCategoryProgressData category in progress.categoryProgressList)
        {
            if (category == null || category.seenSetIds == null)
                continue;

            foreach (string setId in category.seenSetIds)
            {
                if (string.IsNullOrWhiteSpace(setId))
                    continue;

                if (dialogueIds.Add(MakeDialogueId(npcId, setId)))
                    changed = true;
            }
        }

        if (!changed)
            return;

        Save(DialogueKeyBase, dialogueIds);
        ProgressChanged?.Invoke();
    }

    public static bool IsWorldRecordDiscovered(string recordId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(recordId) &&
               worldRecordIds.Contains(recordId);
    }

    public static void MarkWorldRecordDiscovered(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
            return;

        EnsureLoaded();

        if (!worldRecordIds.Add(recordId))
            return;

        Save(WorldRecordKeyBase, worldRecordIds);
        ProgressChanged?.Invoke();
    }

    public static void ReloadForCurrentSlot()
    {
        cachedSlot = null;
        EnsureLoaded();
        ProgressChanged?.Invoke();
        UnreadChanged?.Invoke();
    }

    private static void EnsureLoaded()
    {
        string slot = PlayerPrefs.GetString("SelectedSave", "");

        if (dialogueIds != null &&
            unreadDialogueIds != null &&
            worldRecordIds != null &&
            string.Equals(cachedSlot, slot, StringComparison.Ordinal))
            return;

        cachedSlot = slot;
        dialogueIds = Load(DialogueKeyBase);
        unreadDialogueIds = Load(UnreadDialogueKeyBase);
        worldRecordIds = Load(WorldRecordKeyBase);
    }

    private static string MakeDialogueId(string npcId, string setId)
    {
        return npcId + "|" + setId;
    }

    private static string GetKey(string baseKey)
    {
        string slot = PlayerPrefs.GetString("SelectedSave", "");
        return string.IsNullOrWhiteSpace(slot)
            ? baseKey
            : slot + ":" + baseKey;
    }

    private static HashSet<string> Load(string baseKey)
    {
        string raw = PlayerPrefs.GetString(GetKey(baseKey), "");

        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>(StringComparer.Ordinal);

        try
        {
            StringListData data = JsonUtility.FromJson<StringListData>(raw);
            return new HashSet<string>(
                data != null && data.values != null
                    ? data.values
                    : new List<string>(),
                StringComparer.Ordinal);
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private static void Save(string baseKey, HashSet<string> values)
    {
        StringListData data = new StringListData
        {
            values = new List<string>(values)
        };

        PlayerPrefs.SetString(GetKey(baseKey), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool HasAnyDialogueDiscovered(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return false;

        EnsureLoaded();

        string prefix = npcId + "|";

        foreach (string id in dialogueIds)
        {
            if (id.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
