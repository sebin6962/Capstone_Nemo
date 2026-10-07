using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

public class LevelUpRevealPanel : MonoBehaviour
{
    [Serializable]
    private class UnlockConfigRoot
    {
        public UnlockLevelData[] levels;
    }

    [Serializable]
    private class UnlockLevelData
    {
        public int level;
        public string[] makers;
        public string[] recipes;
        public string[] shopItems;
    }

    [Serializable]
    private class DoGamDataRoot
    {
        public DoGamEntry[] entries;
    }

    [Serializable]
    private class DoGamEntry
    {
        public string name;
        public string image;
        public string nameKey;
    }

    [Serializable]
    private class MakerJsonRoot
    {
        public MakerJsonItem[] items;
    }

    [Serializable]
    private class MakerJsonItem
    {
        public string name;
        public string image;
        public string desc;
        public string nameKey;
        public string descKey;
    }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Unlock Sections")]
    [Tooltip("'새로운 다과' 섹션 전체 Root")]
    [SerializeField] private GameObject recipeSectionRoot;
    [SerializeField] private Transform recipeSlotsParent;
    [SerializeField] private GameObject recipeSlotPrefab;

    [Tooltip("'새로운 제작대' 섹션 전체 Root")]
    [SerializeField] private GameObject makerSectionRoot;
    [SerializeField] private Transform makerSlotsParent;
    [SerializeField] private GameObject makerSlotPrefab;

    [Tooltip("'새로운 재료' 섹션 전체 Root")]
    [SerializeField] private GameObject ingredientSectionRoot;
    [SerializeField] private Transform ingredientSlotsParent;
    [SerializeField] private GameObject ingredientSlotPrefab;

    [Header("Legacy Slots (기존 Inspector 호환)")]
    [Tooltip("recipeSlotsParent를 비워두면 기존 slotsParent를 다과 슬롯 Parent로 사용합니다.")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private Transform slotBackgroundParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Unlock Data")]
    [Tooltip("레벨별 실제 해금 기준 데이터. UnlockConfig.json을 연결하세요.")]
    [SerializeField] private TextAsset unlockConfigJson;

    [Tooltip("완성 다과 판별용. DoGamData.json을 연결하세요.")]
    [SerializeField] private TextAsset doGamDataJson;

    [Tooltip("제작대 이름/이미지/설명용. Maker.json을 연결하세요.")]
    [SerializeField] private TextAsset makerJson;

    [Header("Sprites")]
    [SerializeField] private SpriteAtlas atlas;
    [SerializeField] private string resourcesPrefix = "";
    [SerializeField] private string[] sheetNames;

    [Header("Level Title (Sprite Mode)")]
    [SerializeField] private bool useCommonLevelUpSprite = true;
    [SerializeField] private Image levelTitleImage;
    [SerializeField] private Sprite commonLevelUpSprite;
    [SerializeField] private string commonLevelUpKey = "ui_level_up";
    [SerializeField] private bool hideLevelTextWhenSprite = true;

    [Header("Level Number")]
    [SerializeField] private Image levelNumberImage1;
    [SerializeField] private Image levelNumberImage10;
    [SerializeField] private Sprite[] levelNumberSprites;

    [Header("Effect")]
    [SerializeField] private ParticleSystemRenderer[] preplacedFxRenderers;

    [Header("Timing")]
    [SerializeField] private float panelDelaySeconds = 1f;
    [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private float slotDelaySeconds = 1.2f;
    [SerializeField] private float slotFadeSeconds = 0.25f;
    [SerializeField] private float fadeOutSeconds = 0.35f;
    [SerializeField] private bool crossfadeWithCutscene = true;
    [SerializeField] private float delayBetweenBgAndSlots = 0.2f;

    [Header("Confirm")]
    [Tooltip("ON이면 자동 종료하지 않고 플레이어 확인을 기다립니다.")]
    [SerializeField] private bool waitForPlayerConfirm = true;
    [SerializeField] private KeyCode confirmKey = KeyCode.E;
    [SerializeField] private Button confirmButton;
    [Tooltip("waitForPlayerConfirm이 OFF일 때만 사용합니다.")]
    [SerializeField] private float autoCloseSeconds = 3f;

    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    private readonly Dictionary<int, UnlockLevelData> _unlockByLevel = new();
    private readonly Dictionary<string, DoGamEntry> _dogamByImage = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MakerJsonItem> _makerByImage = new(StringComparer.OrdinalIgnoreCase);

    private CanvasGroup _cgPanel;
    private CanvasGroup _cgSlotBackground;
    private bool _confirmRequested;

    private void Awake()
    {
        SetFxRenderers(false);
        ParseAllData();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(RequestConfirm);
            confirmButton.onClick.AddListener(RequestConfirm);
        }
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(RequestConfirm);
    }

    /// <summary>
    /// 기존 호출부 호환용입니다.
    /// finishKeys는 UnlockConfig를 읽지 못했을 때만 fallback으로 사용합니다.
    /// 정상 상태에서는 UnlockConfig.json을 단일 기준으로 사용합니다.
    /// </summary>
    public void Show(int level, IList<string> finishKeys, Action onComplete)
    {
        ShowInternal(level, finishKeys, onComplete);
    }

    /// <summary>
    /// 새 호출부에서는 finishKeys 없이 레벨만 넘겨도 됩니다.
    /// </summary>
    public void Show(int level, Action onComplete)
    {
        ShowInternal(level, null, onComplete);
    }

    private void ShowInternal(int level, IList<string> legacyFinishKeys, Action onComplete)
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        // 에디터에서 TextAsset을 교체한 뒤 Play를 시작했을 때도 안전하게 최신 데이터를 사용.
        if (_unlockByLevel.Count == 0 || _dogamByImage.Count == 0 || _makerByImage.Count == 0)
            ParseAllData();

        _cgPanel = panelRoot.GetComponent<CanvasGroup>() ?? panelRoot.AddComponent<CanvasGroup>();
        _cgPanel.alpha = 0f;
        _cgPanel.interactable = false;
        _cgPanel.blocksRaycasts = false;

        if (slotBackgroundParent != null)
        {
            GameObject bg = slotBackgroundParent.gameObject;
            _cgSlotBackground = bg.GetComponent<CanvasGroup>() ?? bg.AddComponent<CanvasGroup>();
            _cgSlotBackground.alpha = 0f;
            bg.SetActive(false);
        }
        else
        {
            _cgSlotBackground = null;
        }

        _confirmRequested = false;
        SetFxRenderers(false);
        StopAllCoroutines();

        BuildUI(level, legacyFinishKeys);

        panelRoot.SetActive(true);
        StartCoroutine(ShowFlow(onComplete));
    }

    private IEnumerator ShowFlow(Action onComplete)
    {
        yield return new WaitForSecondsRealtime(panelDelaySeconds);

        if (SFXManager.Instance)
            SFXManager.Instance.PlayLevelUpSFX();

        SetFxRenderers(true);
        yield return Fade(_cgPanel, 0f, 1f, fadeInSeconds);

        yield return new WaitForSecondsRealtime(slotDelaySeconds);

        if (_cgSlotBackground != null)
        {
            _cgSlotBackground.gameObject.SetActive(true);
            yield return Fade(_cgSlotBackground, 0f, 1f, slotFadeSeconds * 0.5f);
            yield return new WaitForSecondsRealtime(delayBetweenBgAndSlots);
        }

        if (SFXManager.Instance)
            SFXManager.Instance.PlayUnlockSlotSFX();

        yield return new WaitForSecondsRealtime(slotFadeSeconds);

        if (waitForPlayerConfirm)
        {
            _cgPanel.interactable = true;
            _cgPanel.blocksRaycasts = true;

            while (!_confirmRequested)
            {
                if (Input.GetKeyDown(confirmKey))
                    _confirmRequested = true;

                yield return null;
            }

            _cgPanel.interactable = false;
            _cgPanel.blocksRaycasts = false;
        }
        else
        {
            yield return new WaitForSecondsRealtime(autoCloseSeconds);
        }

        if (crossfadeWithCutscene)
            onComplete?.Invoke();

        SetFxRenderers(false);
        yield return Fade(_cgPanel, 1f, 0f, fadeOutSeconds);

        if (!crossfadeWithCutscene)
            onComplete?.Invoke();

        panelRoot.SetActive(false);
    }

    public void RequestConfirm()
    {
        if (!waitForPlayerConfirm) return;
        _confirmRequested = true;
    }

    private void BuildUI(int level, IList<string> legacyFinishKeys)
    {
        SetupLevelTitle(level);

        Transform recipeParent = recipeSlotsParent != null ? recipeSlotsParent : slotsParent;
        GameObject recipePrefab = recipeSlotPrefab != null ? recipeSlotPrefab : slotPrefab;

        ClearChildren(recipeParent);
        ClearChildren(makerSlotsParent);
        ClearChildren(ingredientSlotsParent);

        List<string> finishedRecipeKeys = new();
        List<string> makerKeys = new();
        List<string> ingredientKeys = new();

        if (_unlockByLevel.TryGetValue(level, out UnlockLevelData levelData) && levelData != null)
        {
            finishedRecipeKeys = GetFinishedRecipeKeys(levelData.recipes);
            makerKeys = GetUniqueMakerKeys(levelData.makers);
            ingredientKeys = GetShopItemKeys(levelData.shopItems);
        }
        else
        {
            // 기존 세이브/호출부에서 UnlockConfig에 없는 레벨을 호출하더라도
            // 기존 다과 표시 기능이 완전히 사라지지 않도록 fallback만 유지.
            finishedRecipeKeys = CleanKeys(legacyFinishKeys).ToList();
            Debug.LogWarning($"[LevelUpRevealPanel] UnlockConfig에 Lv.{level} 데이터가 없습니다. 기존 finishKeys를 fallback으로 사용합니다.");
        }

        int recipeCount = BuildRecipeSlots(recipeParent, recipePrefab, finishedRecipeKeys);
        int makerCount = BuildMakerSlots(makerSlotsParent, makerSlotPrefab, makerKeys);
        int ingredientCount = BuildIngredientSlots(ingredientSlotsParent, ingredientSlotPrefab, ingredientKeys);

        SetSectionActive(recipeSectionRoot, recipeParent, recipeCount > 0);
        SetSectionActive(makerSectionRoot, makerSlotsParent, makerCount > 0);
        SetSectionActive(ingredientSectionRoot, ingredientSlotsParent, ingredientCount > 0);
    }

    private List<string> GetFinishedRecipeKeys(IEnumerable<string> recipeKeys)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string key in CleanKeys(recipeKeys))
        {
            // DoGamData의 image로 등록된 항목만 완성 다과로 취급.
            // 반죽/가열/절단 등의 중간 제작물은 자동 제외됩니다.
            if (_dogamByImage.ContainsKey(key) && seen.Add(key))
                result.Add(key);
        }

        return result;
    }

    private List<string> GetUniqueMakerKeys(IEnumerable<string> makerKeys)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string raw in CleanKeys(makerKeys))
        {
            string normalized = NormalizeMakerKey(raw);
            if (string.IsNullOrWhiteSpace(normalized)) continue;

            if (seen.Add(normalized))
                result.Add(normalized);
        }

        return result;
    }

    private static List<string> GetShopItemKeys(IEnumerable<string> shopItems)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string raw in CleanKeys(shopItems))
        {
            string key = raw;
            int colon = raw.IndexOf(':');
            if (colon >= 0 && colon < raw.Length - 1)
                key = raw.Substring(colon + 1).Trim();

            if (!string.IsNullOrWhiteSpace(key) && seen.Add(key))
                result.Add(key);
        }

        return result;
    }

    private int BuildRecipeSlots(Transform parent, GameObject prefab, IList<string> keys)
    {
        if (parent == null || prefab == null || keys == null) return 0;

        int count = 0;
        foreach (string key in CleanKeys(keys))
        {
            GameObject go = Instantiate(prefab, parent);
            SetSlotImage(go, key);
            SetSlotName(go, GetRecipeDisplayName(key));
            count++;
        }

        return count;
    }

    private int BuildMakerSlots(Transform parent, GameObject prefab, IList<string> keys)
    {
        if (parent == null || prefab == null || keys == null) return 0;

        int count = 0;
        foreach (string key in CleanKeys(keys))
        {
            GameObject go = Instantiate(prefab, parent);
            MakerJsonItem maker = FindMaker(key);

            string spriteKey = maker != null && !string.IsNullOrWhiteSpace(maker.image)
                ? maker.image
                : key;

            string displayName = maker != null && !string.IsNullOrWhiteSpace(maker.name)
                ? maker.name
                : GetItemDisplayName(key);

            string description = maker != null ? maker.desc : string.Empty;

            SetSlotImage(go, spriteKey);
            SetSlotName(go, displayName);
            SetSlotDescription(go, description);
            count++;
        }

        return count;
    }

    private int BuildIngredientSlots(Transform parent, GameObject prefab, IList<string> keys)
    {
        if (parent == null || prefab == null || keys == null) return 0;

        int count = 0;
        foreach (string key in CleanKeys(keys))
        {
            GameObject go = Instantiate(prefab, parent);
            SetSlotImage(go, key);
            SetSlotName(go, GetItemDisplayName(key));
            count++;
        }

        return count;
    }

    private string GetRecipeDisplayName(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        if (_dogamByImage.TryGetValue(key, out DoGamEntry entry)
            && entry != null
            && !string.IsNullOrWhiteSpace(entry.name))
        {
            return entry.name;
        }

        return GetItemDisplayName(key);
    }

    private static string GetItemDisplayName(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        if (ItemTooltipDB.TooltipTexts.TryGetValue(key, out string localizedName)
            && !string.IsNullOrWhiteSpace(localizedName))
        {
            return localizedName;
        }

        // UnlockConfig의 대소문자와 Tooltip DB 키가 다른 경우를 대비한 fallback.
        foreach (var pair in ItemTooltipDB.TooltipTexts)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(pair.Value))
            {
                return pair.Value;
            }
        }

        return key;
    }

    private static string NormalizeMakerKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        string key = raw.Trim();

        // UnlockConfig의 실제 makerId와 Maker.json의 image 키 차이를 정규화.
        if (key.StartsWith("Mixing", StringComparison.OrdinalIgnoreCase))
            return "MixingBowl";

        if (key.StartsWith("Sieve", StringComparison.OrdinalIgnoreCase))
            return "Sieve";

        if (key.StartsWith("Siru", StringComparison.OrdinalIgnoreCase))
            return "Siru";

        if (key.StartsWith("Pot", StringComparison.OrdinalIgnoreCase))
            return "Pot";

        return key.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
    }

    private MakerJsonItem FindMaker(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        string normalized = NormalizeMakerKey(key);
        return _makerByImage.TryGetValue(normalized, out MakerJsonItem maker) ? maker : null;
    }

    private void ParseAllData()
    {
        ParseUnlockConfig();
        ParseDoGamData();
        ParseMakerJson();
    }

    private void ParseUnlockConfig()
    {
        _unlockByLevel.Clear();

        if (unlockConfigJson == null || string.IsNullOrWhiteSpace(unlockConfigJson.text))
        {
            Debug.LogWarning("[LevelUpRevealPanel] UnlockConfig.json이 연결되지 않았습니다.");
            return;
        }

        try
        {
            UnlockConfigRoot root = JsonUtility.FromJson<UnlockConfigRoot>(unlockConfigJson.text);
            if (root?.levels == null) return;

            foreach (UnlockLevelData data in root.levels)
            {
                if (data == null) continue;
                _unlockByLevel[data.level] = data;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelUpRevealPanel] UnlockConfig.json parse failed: {e.Message}");
        }
    }

    private void ParseDoGamData()
    {
        _dogamByImage.Clear();

        if (doGamDataJson == null || string.IsNullOrWhiteSpace(doGamDataJson.text))
        {
            Debug.LogWarning("[LevelUpRevealPanel] DoGamData.json이 연결되지 않았습니다. 완성 다과 자동 필터링이 불가능합니다.");
            return;
        }

        try
        {
            DoGamDataRoot root = JsonUtility.FromJson<DoGamDataRoot>(doGamDataJson.text);
            if (root?.entries == null) return;

            foreach (DoGamEntry entry in root.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.image)) continue;
                _dogamByImage[entry.image] = entry;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelUpRevealPanel] DoGamData.json parse failed: {e.Message}");
        }
    }

    private void ParseMakerJson()
    {
        _makerByImage.Clear();

        if (makerJson == null || string.IsNullOrWhiteSpace(makerJson.text))
        {
            Debug.LogWarning("[LevelUpRevealPanel] Maker.json이 연결되지 않았습니다.");
            return;
        }

        try
        {
            MakerJsonRoot root = JsonUtility.FromJson<MakerJsonRoot>(makerJson.text);
            if (root?.items == null) return;

            foreach (MakerJsonItem item in root.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.image)) continue;
                _makerByImage[item.image] = item;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LevelUpRevealPanel] Maker.json parse failed: {e.Message}");
        }
    }

    private void SetFxRenderers(bool on)
    {
        if (preplacedFxRenderers == null) return;

        foreach (ParticleSystemRenderer renderer in preplacedFxRenderers)
        {
            if (renderer != null)
                renderer.enabled = on;
        }
    }

    private static IEnumerator Fade(CanvasGroup cg, float from, float to, float sec)
    {
        if (cg == null) yield break;

        if (sec <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.SmoothStep(from, to, Mathf.Clamp01(t / sec));
            yield return null;
        }

        cg.alpha = to;
    }

    private static IEnumerable<string> CleanKeys(IEnumerable<string> keys)
    {
        if (keys == null) yield break;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in keys)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string key = raw.Trim();
            if (seen.Add(key))
                yield return key;
        }
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static void SetSectionActive(GameObject explicitRoot, Transform fallbackParent, bool active)
    {
        if (explicitRoot != null)
            explicitRoot.SetActive(active);
        else if (fallbackParent != null)
            fallbackParent.gameObject.SetActive(active);
    }

    private void SetSlotImage(GameObject slot, string spriteKey)
    {
        if (slot == null) return;

        Transform imageTransform = slot.transform.Find("UnlockImage");
        if (imageTransform == null)
            imageTransform = slot.transform.Find("Image");

        if (imageTransform == null) return;

        Image image = imageTransform.GetComponent<Image>();
        if (image == null) return;

        Sprite sprite = ResolveSprite(spriteKey);
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static void SetSlotName(GameObject slot, string value)
    {
        if (slot == null) return;

        Transform nameTransform = slot.transform.Find("UnlockName");
        if (nameTransform == null)
            nameTransform = slot.transform.Find("Name");

        if (nameTransform == null) return;

        TMP_Text text = nameTransform.GetComponent<TMP_Text>();
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static void SetSlotDescription(GameObject slot, string value)
    {
        if (slot == null) return;

        Transform descTransform = slot.transform.Find("UnlockDescription");
        if (descTransform == null)
            descTransform = slot.transform.Find("Description");

        if (descTransform == null) return;

        TMP_Text text = descTransform.GetComponent<TMP_Text>();
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private void SetupLevelTitle(int level)
    {
        Sprite titleSprite = null;

        if (useCommonLevelUpSprite && commonLevelUpSprite != null)
            titleSprite = commonLevelUpSprite;

        if (useCommonLevelUpSprite && titleSprite == null && !string.IsNullOrEmpty(commonLevelUpKey))
            titleSprite = ResolveSprite(commonLevelUpKey);

        bool hasSprite = titleSprite != null;

        if (useCommonLevelUpSprite && levelTitleImage != null && hasSprite)
        {
            levelTitleImage.sprite = titleSprite;
            levelTitleImage.enabled = true;

            if (hideLevelTextWhenSprite && levelText != null)
                levelText.gameObject.SetActive(false);
        }
        else
        {
            if (levelText != null)
            {
                levelText.text = "LEVEL UP!";
                levelText.gameObject.SetActive(true);
            }

            if (levelTitleImage != null)
                levelTitleImage.enabled = false;
        }

        SetLevelNumber(level);
    }

    public void SetLevelNumber(int level)
    {
        if (levelNumberSprites == null || levelNumberSprites.Length < 10)
            return;

        int ones = level % 10;
        int tens = (level / 10) % 10;

        if (levelNumberImage1 != null)
        {
            levelNumberImage1.gameObject.SetActive(true);
            levelNumberImage1.sprite = levelNumberSprites[ones];
            levelNumberImage1.enabled = true;
        }

        if (levelNumberImage10 != null)
        {
            if (level >= 10)
            {
                levelNumberImage10.gameObject.SetActive(true);
                levelNumberImage10.sprite = levelNumberSprites[tens];
                levelNumberImage10.enabled = true;
            }
            else
            {
                levelNumberImage10.gameObject.SetActive(false);
                levelNumberImage10.enabled = false;
            }
        }
    }

    private Sprite ResolveSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        if (_spriteCache.TryGetValue(key, out Sprite cached))
            return cached;

        Sprite sprite = null;

        if (atlas != null)
        {
            sprite = atlas.GetSprite(key);
            if (sprite != null)
                return _spriteCache[key] = sprite;
        }

        if (!string.IsNullOrEmpty(resourcesPrefix))
        {
            sprite = Resources.Load<Sprite>($"{resourcesPrefix}/{key}");
            if (sprite != null)
                return _spriteCache[key] = sprite;
        }

        if (sheetNames != null)
        {
            foreach (string sheet in sheetNames)
            {
                if (string.IsNullOrEmpty(sheet)) continue;

                Sprite[] all = Resources.LoadAll<Sprite>(
                    string.IsNullOrEmpty(resourcesPrefix)
                        ? sheet
                        : $"{resourcesPrefix}/{sheet}"
                );

                if (all == null || all.Length == 0) continue;

                sprite = all.FirstOrDefault(sp => sp != null && sp.name == key);
                if (sprite != null)
                    return _spriteCache[key] = sprite;
            }
        }

        Debug.LogWarning($"[LevelUpRevealPanel] Sprite not found: {key}");
        return null;
    }

#if UNITY_EDITOR
    [Header("Debug Test")]
    [SerializeField, Range(1, 14)] private int debugTestLevel = 2;

    [Tooltip("에디터 Play Mode에서만 사용됩니다. F6=이전 레벨, F7=다음 레벨, F8=현재 레벨 패널 테스트")]
    [SerializeField] private bool enableDebugHotkeys = true;

    private void Update()
    {
        if (!Application.isPlaying || !enableDebugHotkeys)
            return;

        // F6 : 이전 레벨
        if (Input.GetKeyDown(KeyCode.F6))
        {
            debugTestLevel = Mathf.Max(1, debugTestLevel - 1);
            Debug.Log($"[LevelUpRevealPanel] Debug Level -> Lv.{debugTestLevel}");
        }

        // F7 : 다음 레벨
        if (Input.GetKeyDown(KeyCode.F7))
        {
            debugTestLevel = Mathf.Min(14, debugTestLevel + 1);
            Debug.Log($"[LevelUpRevealPanel] Debug Level -> Lv.{debugTestLevel}");
        }

        // F8 : 현재 선택 레벨 패널 실행
        if (Input.GetKeyDown(KeyCode.F8))
        {
            DebugTestUnlockPanel();
        }
    }

    [ContextMenu("DEBUG - Test Unlock Panel")]
    private void DebugTestUnlockPanel()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play Mode에서 실행해주세요.");
            return;
        }

        int testLevel = debugTestLevel;

        Show(testLevel, () =>
        {
            Debug.Log($"[LevelUpRevealPanel] Lv.{testLevel} 테스트 완료");
        });
    }
#endif
}