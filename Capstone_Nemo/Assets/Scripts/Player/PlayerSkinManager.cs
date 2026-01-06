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
        // 씬 로드 직후엔 플레이어가 아직 없을 수 있어서 한 프레임 뒤에 적용
        StartCoroutine(RebindAndApplyNextFrame());
    }

    private IEnumerator RebindAndApplyNextFrame()
    {
        yield return null;

        // 새 씬의 플레이어 SpriteLibrary로 다시 바인딩
        targetLibrary = FindObjectOfType<SpriteLibrary>();

        // 현재 저장된 착용 스킨을 다시 적용 (save=false)
        Apply(_data.equippedIndex, save: false);
    }

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

        _path = Path.Combine(Application.persistentDataPath, $"playerSkin_{serverName}.json");

        if (targetLibrary == null)
            targetLibrary = FindObjectOfType<SpriteLibrary>();

        LoadOrCreate();
        Apply(_data.equippedIndex, save: false);
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

        // SpendStarlight는 음수 방지 체크가 없음 → UI/로직에서 먼저 체크 필요 :contentReference[oaicite:1]{index=1}
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

            // 라이브러리 교체 후, Resolver들 강제 갱신
            var resolvers = targetLibrary.GetComponentsInChildren<SpriteResolver>(true);
            foreach (var r in resolvers)
            {
                try { r.ResolveSpriteToSpriteRenderer(); } catch { /* 버전차 방어 */ }
            }
        }

        _data.equippedIndex = idx;
        if (save) Save();
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
