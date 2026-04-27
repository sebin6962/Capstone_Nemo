using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.SceneManagement;

public class PlayerSkinManager : MonoBehaviour
{
    public static PlayerSkinManager Instance;

    public int EquippedIndex => _data.equippedIndex;

    [Serializable]
    public class SkinSaveData
    {
        public int equippedIndex = 0;
        public List<int> ownedIndexes = new List<int> { 0 }; // 기본 스킨은 항상 소유

        // 석상 색상 변경 저장값
        public bool hasCustomColor = false;
        public float colorR = 1f;
        public float colorG = 1f;
        public float colorB = 1f;
        public float colorA = 1f;
    }

    [Serializable]
    public class SkinDefinition
    {
        public string id;
        public int price;
        public Sprite preview;
        public SpriteLibraryAsset libraryAsset; // 스킨별 SpriteLibraryAsset
    }

    [Header("스킨 목록 (0번 = 기본 스킨)")]
    public List<SkinDefinition> skins = new List<SkinDefinition>();

    [Header("적용 대상 SpriteLibrary (없으면 플레이어에서 자동 탐색)")]
    public SpriteLibrary targetLibrary;

    [Header("색상 제외 대상 SpriteRenderers")]
    [Tooltip("그림자처럼 색상이 바뀌면 안 되는 SpriteRenderer를 직접 넣을 수 있습니다.")]
    public SpriteRenderer[] colorExcludeTargets;

    [Header("색상 제외 이름 키워드")]
    public string[] colorExcludeNameKeywords = { "Shadow", "shadow", "그림자" };

    [Header("색상 적용 대상 SpriteRenderers (비워두면 targetLibrary 하위에서 자동 탐색)")]
    public SpriteRenderer[] colorTargets;

    private SkinSaveData _data = new SkinSaveData();
    private string _path = "";

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindAndApplyNextFrame());
    }

    private IEnumerator RebindAndApplyNextFrame()
    {
        yield return null;

        string selectedSave = PlayerPrefs.GetString("SelectedSave", string.Empty);

        // 선택된 세이브가 바뀌었는데 아직 스킨 매니저가 이전 세이브를 들고 있으면 다시 로드
        if (!string.IsNullOrEmpty(selectedSave) && selectedSave != _currentServerName)
        {
            SwitchToSave(selectedSave);
            yield break;
        }

        // 새 씬의 플레이어 SpriteLibrary로 다시 바인딩
        targetLibrary = FindObjectOfType<SpriteLibrary>();

        // 현재 선택된 세이브의 스킨/색상을 다시 적용
        Apply(_data.equippedIndex, save: false);
    }

    private string _currentServerName = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitFromSelectedSave();
        }
        else Destroy(gameObject);
    }

    public void InitFromSelectedSave()
    {
        var serverName = PlayerPrefs.GetString("SelectedSave", string.Empty);
        if (string.IsNullOrEmpty(serverName))
        {
            Debug.LogWarning("[Skin] SelectedSave is empty.");
            return;
        }

        SwitchToSave(serverName);
    }

    public void SwitchToSave(string serverName)
    {
        if (string.IsNullOrEmpty(serverName))
        {
            Debug.LogWarning("[Skin] serverName is empty.");
            return;
        }

        _currentServerName = serverName;
        _path = Path.Combine(Application.persistentDataPath, $"playerSkin_{serverName}.json");

        // 중요: 이전 세이브의 스킨/색상 데이터가 남지 않도록 먼저 초기화
        _data = new SkinSaveData();

        targetLibrary = FindObjectOfType<SpriteLibrary>();

        LoadOrCreate();

        // 현재 씬에 플레이어가 있으면 바로 적용,
        // SaveSelectScene처럼 플레이어가 없어도 데이터만 새로 로드됨
        Apply(_data.equippedIndex, save: false);

        Debug.Log($"[Skin] SwitchToSave: {serverName}");
    }

    public bool IsOwned(int idx)
    {
        if (idx == 0) return true;
        return _data.ownedIndexes != null && _data.ownedIndexes.Contains(idx);
    }

    public int GetPrice(int idx)
    {
        if (idx < 0 || idx >= skins.Count) return int.MaxValue;
        return Mathf.Max(0, skins[idx].price);
    }

    public Sprite GetPreview(int idx)
    {
        if (idx < 0 || idx >= skins.Count) return null;
        return skins[idx].preview;
    }

    public bool TryBuy(int idx, out string failReason)
    {
        failReason = "";

        idx = Mathf.Clamp(idx, 0, skins.Count - 1);
        if (IsOwned(idx)) return true;

        var star = StarDataManager.Instance;
        if (star == null)
        {
            failReason = "별빛 데이터를 찾을 수 없어요.";
            return false;
        }

        int price = GetPrice(idx);

        if (star.playerData.starlight < price)
        {
            failReason = "별빛이 부족해요.";
            return false;
        }

        star.SpendStarlight(price);

        if (_data.ownedIndexes == null) _data.ownedIndexes = new List<int> { 0 };
        if (!_data.ownedIndexes.Contains(idx)) _data.ownedIndexes.Add(idx);
        Save();

        return true;
    }

    public void Apply(int idx, bool save = true)
    {
        if (skins == null || skins.Count == 0) return;

        idx = Mathf.Clamp(idx, 0, skins.Count - 1);

        if (targetLibrary == null)
            targetLibrary = FindObjectOfType<SpriteLibrary>();

        if (targetLibrary != null && skins[idx].libraryAsset != null)
        {
            targetLibrary.spriteLibraryAsset = skins[idx].libraryAsset;

            var resolvers = targetLibrary.GetComponentsInChildren<SpriteResolver>(true);
            foreach (var r in resolvers)
            {
                try { r.ResolveSpriteToSpriteRenderer(); } catch { }
            }
        }

        _data.equippedIndex = idx;

        // 스킨을 바꿔도 저장된 색상 유지
        ApplySavedColor();

        if (save) Save();
    }

    public void SetPlayerColor(Color color, bool save = true)
    {
        _data.hasCustomColor = true;
        _data.colorR = color.r;
        _data.colorG = color.g;
        _data.colorB = color.b;
        _data.colorA = color.a;

        ApplyColorToPlayer(color);

        if (save) Save();
    }

    public void ResetPlayerColor(bool save = true)
    {
        _data.hasCustomColor = false;
        _data.colorR = 1f;
        _data.colorG = 1f;
        _data.colorB = 1f;
        _data.colorA = 1f;

        ApplyColorToPlayer(Color.white);

        if (save) Save();
    }
    public Color GetSavedColor()
    {
        if (!_data.hasCustomColor)
            return Color.white;

        return new Color(_data.colorR, _data.colorG, _data.colorB, _data.colorA);
    }

    private void ApplySavedColor()
    {
        ApplyColorToPlayer(GetSavedColor());
    }

    private void ApplyColorToPlayer(Color color)
    {
        SpriteRenderer[] targets = colorTargets;

        // colorTargets를 직접 지정하지 않았다면,
        // 현재 플레이어 SpriteLibrary 하위 SpriteRenderer들을 자동 탐색
        if (targets == null || targets.Length == 0)
        {
            if (targetLibrary == null)
                targetLibrary = FindObjectOfType<SpriteLibrary>();

            if (targetLibrary != null)
                targets = targetLibrary.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (targets == null) return;

        foreach (var sr in targets)
        {
            if (sr == null) continue;

            // 그림자/제외 대상은 색상 변경하지 않음
            if (IsColorExcluded(sr)) continue;

            sr.color = color;
        }
    }

    private bool IsColorExcluded(SpriteRenderer sr)
    {
        if (sr == null) return true;

        // 직접 제외 목록에 들어간 SpriteRenderer면 제외
        if (colorExcludeTargets != null)
        {
            foreach (var exclude in colorExcludeTargets)
            {
                if (exclude != null && exclude == sr)
                    return true;
            }
        }

        // 오브젝트 이름에 제외 키워드가 들어가면 제외
        if (colorExcludeNameKeywords != null)
        {
            string objName = sr.gameObject.name;

            foreach (string keyword in colorExcludeNameKeywords)
            {
                if (string.IsNullOrEmpty(keyword)) continue;

                if (objName.Contains(keyword))
                    return true;
            }
        }

        return false;
    }

    private void LoadOrCreate()
    {
        if (!File.Exists(_path))
        {
            _data = new SkinSaveData();
            Save();
            return;
        }

        try
        {
            _data = JsonUtility.FromJson<SkinSaveData>(File.ReadAllText(_path));
        }
        catch
        {
            _data = new SkinSaveData();
        }

        if (_data.ownedIndexes == null) _data.ownedIndexes = new List<int> { 0 };
        if (!_data.ownedIndexes.Contains(0)) _data.ownedIndexes.Add(0);
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(_path)) return;
        File.WriteAllText(_path, JsonUtility.ToJson(_data, true));
    }
}
