using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using UnityEditorInternal;

[Serializable] class UnlockLevelEntry { public int level; public List<string> makers; public List<string> recipes; }
[Serializable] class UnlockConfig { public List<UnlockLevelEntry> levels; }

[Serializable] class UnlockSaveData
{
    public HashSet<string> unlockedMakers = new();
    public HashSet<string> unlockedRecipes = new();
    public HashSet<int> pendingLevels = new();
    public HashSet<int> appliedLevels = new();  // 추가
    public bool initialized;
}
public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance;
    private UnlockConfig config;
    private UnlockSaveData save = new();
    private string savePath;

    private HashSet<int> _lastAppliedLevels = new(); // 새벽에 실제로 적용된 레벨들
    private bool _revealShownToday = false;          // 오늘 레벨업 패널을 이미 보여줬는지

    const string PP_RevealLevels = "Unlock_RevealLevels_Today";
    const string PP_RevealShown = "Unlock_RevealShown_Today";
    public bool IsMakerUnlocked(string makerId) => save.unlockedMakers.Contains(makerId);
    public bool IsRecipeUnlocked(string recipeKey) => save.unlockedRecipes.Contains(recipeKey);

    static string Norm(string s) => (s ?? "").Trim().ToLowerInvariant();

    void Awake()
    {
        if(Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Path.Combine(Application.persistentDataPath, "unlock_state.json");
        LoadConfig();
        LoadState();

        if (!save.initialized)
        {
            save.appliedLevels.Add(1);          // ★ 기본 레벨 1은 항상 적용
            RebuildUnlockedFromApplied();
            save.initialized = true;
            SaveState();
        }
    }

    void RebuildUnlockedFromApplied()
    {
        save.unlockedMakers.Clear();
        save.unlockedRecipes.Clear();
        foreach (var lv in save.appliedLevels)
        {
            var e = config.levels.FirstOrDefault(x => x.level == lv);
            if (e == null) continue;
            if (e.makers != null) foreach (var m in e.makers) save.unlockedMakers.Add(Norm(m));
            if (e.recipes != null) foreach (var r in e.recipes) save.unlockedRecipes.Add(Norm(r));
        }
        SaveState();
    }

    void Start()
    {
        RefreshMakerActivationInScene();
    }

    void LoadConfig()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/UnlockConfig");
        if (json == null) { Debug.LogError("[Unlock] UnlockConfig.json을 찾을 수 없음"); config = new UnlockConfig { levels = new() }; return; }
        config = JsonUtility.FromJson<UnlockConfig>(json.text) ?? new UnlockConfig { levels = new() };
    }

    void LoadState()
    {
        if (File.Exists(savePath))
        {
            try { save = JsonUtility.FromJson<UnlockSaveData>(File.ReadAllText(savePath)) ?? new UnlockSaveData(); }
            catch { save = new UnlockSaveData(); }
        }
    }

    void SaveState()
    {
        File.WriteAllText(savePath, JsonUtility.ToJson(save, true));
    }
    // 레벨업 "즉시 해금"이 아니라 "다음 날 적용" 예약만
    public void ScheduleUnlockForLevel(int level)
    {
        if (!config.levels.Any(e => e.level == level))
        {
            Debug.LogWarning($"[Unlock] UnlockConfig에 level {level} 항목이 없어 예약 실패");
            return;
        }
        if (save.pendingLevels.Add(level))
        {
            Debug.Log($"[Unlock] 레벨 {level} 해금 예약 완료 (다음 날 적용)");
            SaveState();
        }
    }

    // TimeManager가 하루 넘길 때 호출
    public void ApplyScheduledUnlocksForNewDay()
    {
        // 오늘 표시용 버퍼에 '적용 예정 레벨'을 먼저 복사
        _lastAppliedLevels.Clear();
        if (save.pendingLevels != null && save.pendingLevels.Count > 0)
        {
            _lastAppliedLevels.UnionWith(save.pendingLevels);
            PersistRevealLevels(save.pendingLevels);   // 추가: 오늘 표시용 영속 버퍼
        }
        _revealShownToday = false;

        if (save.pendingLevels.Count == 0) { ClearRevealLevelsIfShown(); return; }

        foreach (int lv in save.pendingLevels.ToList())
        {
            save.appliedLevels.Add(lv);
            save.pendingLevels.Remove(lv);
        }
        RebuildUnlockedFromApplied();           // ★ 항상 한 번에 재계산
        RefreshMakerActivationInScene();
        SaveState();
        // 씬상 제작기 활성화/비활성 동기화
        RefreshMakerActivationInScene();
    }

    public bool HasLevelUpRevealToShow()
    {
        if (HasPendingLevelUps()) return true;
        if (_lastAppliedLevels.Count > 0 && !_revealShownToday) return true;
        var persisted = LoadRevealLevels();
        return persisted.Count > 0 && PlayerPrefs.GetInt(PP_RevealShown, 0) == 0;
    }

    public int GetLevelUpRevealLevel()
    {
        if (HasPendingLevelUps()) return GetHighestPendingLevel();
        if (_lastAppliedLevels.Count > 0) return _lastAppliedLevels.Max();
        var persisted = LoadRevealLevels();
        return persisted.Count > 0 ? Mathf.Max(persisted.ToArray()) : 0;
    }

    public void RefreshMakerActivationInScene()
    {
        foreach (var maker in FindObjectsOfType<MakerInfo>(true))
        { // makerId로 판정
            bool unlocked = IsMakerUnlocked(maker.makerId);
            maker.ApplyLockState(!unlocked); // 잠금이면 true
        }
    }

    public bool HasPendingLevelUps()
    {
        return save != null && save.pendingLevels != null && save.pendingLevels.Count > 0;
    }

    public int GetHighestPendingLevel()
    {
        if (!HasPendingLevelUps()) return 0;
        return save.pendingLevels.Max();
    }

    // 해금 미리보기 중 _finish키 스프라이트만 모음
    public List<string> GetLevelUpRevealFinishKeys()
    {
        IEnumerable<int> levels;
        if (HasPendingLevelUps()) levels = save.pendingLevels;
        else if (_lastAppliedLevels.Count > 0) levels = _lastAppliedLevels;
        else levels = LoadRevealLevels();

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var lv in levels)
        {
            var entry = config.levels.FirstOrDefault(e => e.level == lv);
            if (entry?.recipes == null) continue;
            foreach (var key in entry.recipes)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var k = key.Trim();
                if (k.EndsWith("_finish", StringComparison.OrdinalIgnoreCase))
                    result.Add(k);
            }
        }
        return new List<string>(result);
    }

    public void MarkLevelUpRevealShown()
    {
        _revealShownToday = true;
        PlayerPrefs.SetInt(PP_RevealShown, 1);  // 영속 플래그
        PlayerPrefs.Save();
    }

    void OnEnable() { TimeManager.OnNewDayStarted += ApplyScheduledUnlocksForNewDay; }
    void OnDisable() { TimeManager.OnNewDayStarted -= ApplyScheduledUnlocksForNewDay; }

    private void PersistRevealLevels(IEnumerable<int> levels)
    {
        PlayerPrefs.SetString(PP_RevealLevels, string.Join(",", levels));
        PlayerPrefs.SetInt(PP_RevealShown, 0);
        PlayerPrefs.Save();
    }

    private List<int> LoadRevealLevels()
    {
        var s = PlayerPrefs.GetString(PP_RevealLevels, "");
        var res = new List<int>();
        if (string.IsNullOrEmpty(s)) return res;
        foreach (var tok in s.Split(','))
        {
            if (int.TryParse(tok, out var lv)) res.Add(lv);
        }
        return res;
    }

    private void ClearRevealLevelsIfShown()
    {
        if (PlayerPrefs.GetInt(PP_RevealShown, 0) == 1)
        {
            PlayerPrefs.DeleteKey(PP_RevealLevels);
            PlayerPrefs.Save();
        }
    }

    public string DebugState()
    {
        var pend = (save?.pendingLevels != null) ? string.Join(",", save.pendingLevels) : "(null)";
        var last = (_lastAppliedLevels != null) ? string.Join(",", _lastAppliedLevels) : "(null)";
        var pers = string.Join(",", LoadRevealLevels());
        var shown = PlayerPrefs.GetInt(PP_RevealShown, 0);
        return $"pending=[{pend}] lastApplied=[{last}] persisted=[{pers}] shownToday={_revealShownToday} PP_Shown={shown}";
    }
}

