using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[Serializable]
public class RecordNPCBookItemData
{
    public string id;
    public string dialogueNpcId;
    public string name;
    public string nameKey;
    public string image;
}

[Serializable]
public class RecordNPCBookData
{
    public List<RecordNPCBookItemData> items = new List<RecordNPCBookItemData>();
}

[Serializable]
public class WorldRecordItemData
{
    public string id;
    public string title;
    public string titleKey;
    public string text;
    public string textKey;
    public string location;
    public string locationKey;
    public string image;

    // -1이면 처음부터 목록 후보에 포함.
    // 0~7이면 현재 계수나무 단계에 도달한 뒤에만 목록에 나타남.
    public int requiredTreeLevel = -1;
}



[Serializable]
public class WorldRecordBookData
{
    public List<WorldRecordItemData> records = new List<WorldRecordItemData>();
}


/// <summary>
/// 기존 NPC 도감 탭을 대체하는 '기록' 탭 컨트롤러.
/// 서브탭:
/// 1) 인물 - NPC별 대화 수집 기록
/// 2) 마을 기록 - 상호작용 기록 아이템
/// </summary>
public class DoGamRecordTabController : MonoBehaviour
{
    private EventTrigger characterSubTabTrigger;
    private EventTrigger worldRecordSubTabTrigger;

    [Header("Locked Icon Visual")]
    [SerializeField] private Material lockedSilhouetteMaterial;

    [SerializeField]
    private Color lockedSilhouetteColor =
        new Color(0.45f, 0.45f, 0.45f, 1f);

    private void ApplyDiscoveryVisual(
    Image image,
    bool discovered,
    Color discoveredColor)
    {
        if (image == null)
            return;

        if (!discovered)
        {
            // 원본 RGB를 버리고 Alpha 형태만 남긴 단색 실루엣
            image.material = lockedSilhouetteMaterial;
            image.color = lockedSilhouetteColor;
        }
        else
        {
            // 다시 일반 UI Sprite 렌더링
            image.material = null;
            image.color = discoveredColor;
        }
    }

    private WorldRecordItemData selectedWorldRecord;

    private class WorldRecordButtonView
    {
        public WorldRecordItemData data;
        public Button button;
        public Image icon;
        public bool discovered;
    }

    private readonly List<WorldRecordButtonView>
        worldRecordViews =
            new List<WorldRecordButtonView>();

    private enum SubTab
    {
        Character,
        WorldRecord
    }

    [Header("Root / Sub Tabs")]
    [SerializeField] private GameObject recordRoot;
    [SerializeField] private Button characterSubTabButton;
    [SerializeField] private Button worldRecordSubTabButton;
    [SerializeField] private Sprite subTabNormalSprite;
    [SerializeField] private Sprite subTabPressedSprite;

    [Header("Character Tab Roots")]
    [Tooltip("인물 탭에서만 보일 왼쪽 페이지 전체 Root")]
    [SerializeField] private GameObject characterLeftRoot;

    [Tooltip("인물 탭에서만 보일 오른쪽 페이지 전체 Root")]
    [SerializeField] private GameObject characterRightRoot;

    [Header("Character - Left Page")]
    [SerializeField] private Transform npcGridParent;
    [SerializeField] private GameObject npcItemPrefab;

    [Header("Character - Right Page Header")]
    [SerializeField] private Image npcDetailImage;
    [SerializeField] private TextMeshProUGUI npcDetailNameText;
    [SerializeField] private TextMeshProUGUI npcProgressText;

    [Header("Character - Dialogue List")]
    [SerializeField] private GameObject dialogueListRoot;
    [SerializeField] private Transform dialogueListParent;
    [SerializeField] private GameObject dialogueItemPrefab;
    [SerializeField] private GameObject dialogueStageDividerPrefab;

    [Header("Character - Dialogue Detail")]
    [SerializeField] private GameObject dialogueDetailRoot;
    [SerializeField] private TextMeshProUGUI dialogueDetailTitleText;
    [SerializeField] private TextMeshProUGUI dialogueDetailBodyText;
    [SerializeField] private Button dialogueDetailBackButton;

    [Header("World Record Tab Roots")]
    [Tooltip("마을 기록 탭에서만 보일 왼쪽 페이지 전체 Root")]
    [SerializeField] private GameObject worldRecordLeftRoot;

    [Tooltip("마을 기록 탭에서만 보일 오른쪽 페이지 전체 Root")]
    [SerializeField] private GameObject worldRecordRightRoot;

    [Header("World Record")]
    [SerializeField] private Transform worldRecordListParent;
    [SerializeField] private GameObject worldRecordItemPrefab;
    [SerializeField] private Image worldRecordDetailImage;
    [SerializeField] private TextMeshProUGUI worldRecordDetailTitleText;
    [SerializeField] private TextMeshProUGUI worldRecordDetailLocationText;
    [SerializeField] private TextMeshProUGUI worldRecordDetailBodyText;
    [SerializeField] private TextMeshProUGUI worldRecordProgressText;

    [Header("Resources (without extension)")]
    [SerializeField] private string npcBookResourcePath = "Data/NPCBook";
    [SerializeField] private string npcDialogueResourcePath = "Data/NPCDialogueData";
    [SerializeField] private string worldRecordResourcePath = "Data/WorldRecordData";
    [SerializeField] private string npcSpriteResourceFolder = "Sprites/NPC";
    [SerializeField] private string worldRecordSpriteResourceFolder = "Sprites/WorldRecord";

    [Header("Localization")]
    [SerializeField] private string npcDialogueLocalizationTable = "NPCDialogue";
    [SerializeField] private string doGamLocalizationTable = "DoGam";

    [Header("Selection Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color undiscoveredColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Fallback Text")]
    [SerializeField] private string lockedDialogueTitle = "????";
    [SerializeField] private string lockedDialogueBody = "아직 나누지 않은 이야기입니다.";
    [SerializeField] private string lockedNpcText = "아직 만나지 않은 토끼입니다.";
    [SerializeField] private string lockedWorldRecordTitle = "????";
    [SerializeField] private string lockedWorldRecordBody = "아직 발견하지 못한 기록입니다.";

    private class NPCButtonView
    {
        public RecordNPCBookItemData data;
        public Button button;
        public Image icon;
    }

    private readonly List<NPCButtonView> npcViews = new List<NPCButtonView>();
    private readonly Dictionary<string, Sprite> npcSprites = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Sprite> recordSprites = new Dictionary<string, Sprite>();

    private List<RecordNPCBookItemData> npcBookItems = new List<RecordNPCBookItemData>();
    private List<WorldRecordItemData> worldRecords = new List<WorldRecordItemData>();
    private readonly Dictionary<string, NPCDialogueData> npcDialogueById =
        new Dictionary<string, NPCDialogueData>(StringComparer.Ordinal);

    private Action<Button> registerHover;
    private RecordNPCBookItemData selectedNpc;
    private SubTab currentSubTab = SubTab.Character;
    private bool loaded;
    private bool npcGridBuilt;

    public bool IsOpen => recordRoot != null && recordRoot.activeSelf;

    public bool IsConfigured =>
        recordRoot != null &&
        characterSubTabButton != null &&
        worldRecordSubTabButton != null &&
        characterLeftRoot != null &&
        characterRightRoot != null &&
        worldRecordLeftRoot != null &&
        worldRecordRightRoot != null &&
        npcGridParent != null &&
        npcItemPrefab != null &&
        npcDetailNameText != null &&
        npcProgressText != null &&
        dialogueListRoot != null &&
        dialogueListParent != null &&
        dialogueItemPrefab != null &&
        dialogueDetailRoot != null &&
        dialogueDetailTitleText != null &&
        dialogueDetailBodyText != null &&
        dialogueDetailBackButton != null &&
        worldRecordListParent != null &&
        worldRecordItemPrefab != null &&
        worldRecordDetailTitleText != null &&
        worldRecordDetailBodyText != null;

    public void Initialize(Action<Button> hoverRegistrar)
    {
        registerHover = hoverRegistrar;

        SetupSubTabPointerClick(
            characterSubTabButton,
            ref characterSubTabTrigger,
            () =>
            {
                Debug.Log("[DoGamRecord] CHARACTER SUB TAB CLICK", this);
                PlayTabSound();
                ShowCharacterSubTab();
            });

        SetupSubTabPointerClick(
            worldRecordSubTabButton,
            ref worldRecordSubTabTrigger,
            () =>
            {
                Debug.Log("[DoGamRecord] WORLD RECORD SUB TAB CLICK", this);
                PlayTabSound();
                ShowWorldRecordSubTab();
            });

        if (dialogueDetailBackButton != null)
        {
            dialogueDetailBackButton.onClick.RemoveAllListeners();

            dialogueDetailBackButton.onClick.AddListener(() =>
            {
                PlayPageSound();
                ShowDialogueList();
            });

            registerHover?.Invoke(dialogueDetailBackButton);
        }
    }

    private void SetupSubTabPointerClick(
    Button button,
    ref EventTrigger cachedTrigger,
    Action action)
    {
        if (button == null)
            return;

        // 기존 Button.onClick은 사용하지 않는다.
        button.onClick.RemoveAllListeners();

        EventTrigger trigger =
            button.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        cachedTrigger = trigger;

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        // 기존 PointerClick만 제거.
        // PointerEnter / PointerExit 같은 hover 이벤트는 보존한다.
        trigger.triggers.RemoveAll(
            x => x != null &&
                 x.eventID == EventTriggerType.PointerClick);

        EventTrigger.Entry clickEntry =
            new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

        clickEntry.callback.AddListener(_ =>
        {
            if (button.interactable)
                action?.Invoke();
        });

        trigger.triggers.Add(clickEntry);

        registerHover?.Invoke(button);
    }

    private void OnEnable()
    {
        DoGamRecordProgress.ProgressChanged += OnRecordProgressChanged;
    }

    private void OnDisable()
    {
        DoGamRecordProgress.ProgressChanged -= OnRecordProgressChanged;
    }

    public void OpenTab()
    {
        if (!IsConfigured)
        {
            Debug.LogWarning("[DoGamRecord] Inspector의 기록 탭 UI 참조를 확인해주세요.", this);
            return;
        }

        if (!loaded)
            LoadData();

        if (!npcGridBuilt)
            BuildNpcGrid();

        recordRoot.SetActive(true);

        // 이전 Play 상태나 인스펙터 활성값이 남아 있어도
        // 기록 탭을 열 때 상태를 강제로 초기화한다.
        SetCharacterRoots(false);
        SetWorldRecordRoots(false);
        ShowCharacterSubTab();

        if (npcBookItems.Count > 0)
            SelectNpc(npcBookItems[0], false);
        else
            ClearNpcRightPage();
    }

    public void CloseTab()
    {
        SetCharacterRoots(false);
        SetWorldRecordRoots(false);

        if (recordRoot != null)
            recordRoot.SetActive(false);
    }

    public void RefreshCurrentView()
    {
        if (!IsOpen)
            return;

        if (currentSubTab == SubTab.Character)
        {
            RefreshNpcButtonColors();
            if (selectedNpc != null)
                SelectNpc(selectedNpc, false);
        }
        else
        {
            BuildWorldRecordList();
        }
    }

    // DoGamUIManager의 기존 locale 변경 호출과 호환하기 위한 별칭.
    public void RefreshDetail()
    {
        RefreshCurrentView();
    }

    public void ShowCharacterSubTab()
    {
        Debug.Log("[DoGamRecord] ShowCharacterSubTab ENTER", this);

        currentSubTab = SubTab.Character;

        SetCharacterRoots(true);
        SetWorldRecordRoots(false);

        Debug.Log(
            "[DoGamRecord] Character 적용 결과 / " +
            $"CharacterLeft={characterLeftRoot?.activeSelf}, " +
            $"CharacterRight={characterRightRoot?.activeSelf}, " +
            $"WorldLeft={worldRecordLeftRoot?.activeSelf}, " +
            $"WorldRight={worldRecordRightRoot?.activeSelf}",
            this);

        ApplySubTabSprites();

        ShowDialogueList();
        RefreshNpcButtonColors();

        if (selectedNpc == null && npcBookItems.Count > 0)
            SelectNpc(npcBookItems[0], false);
        else if (selectedNpc != null)
            SelectNpc(selectedNpc, false);

        ForceCharacterLayout();
    }

    public void ShowWorldRecordSubTab()
    {
        Debug.Log("[DoGamRecord] ShowWorldRecordSubTab ENTER", this);

        currentSubTab = SubTab.WorldRecord;

        SetCharacterRoots(false);
        SetWorldRecordRoots(true);

        Debug.Log(
            "[DoGamRecord] WorldRecord 적용 결과 / " +
            $"CharacterLeft={characterLeftRoot?.activeSelf}, " +
            $"CharacterRight={characterRightRoot?.activeSelf}, " +
            $"WorldLeft={worldRecordLeftRoot?.activeSelf}, " +
            $"WorldRight={worldRecordRightRoot?.activeSelf}",
            this);

        ApplySubTabSprites();

        if (dialogueDetailRoot != null)
            dialogueDetailRoot.SetActive(false);

        BuildWorldRecordList();
        ForceWorldRecordLayout();
    }

    private void SetCharacterRoots(bool active)
    {
        if (characterLeftRoot != null)
            characterLeftRoot.SetActive(active);

        if (characterRightRoot != null)
            characterRightRoot.SetActive(active);
    }

    private void SetWorldRecordRoots(bool active)
    {
        if (worldRecordLeftRoot != null)
            worldRecordLeftRoot.SetActive(active);

        if (worldRecordRightRoot != null)
            worldRecordRightRoot.SetActive(active);
    }

    private void ForceCharacterLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (npcGridParent is RectTransform npcRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(npcRect);

        if (dialogueListParent is RectTransform dialogueRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueRect);
    }

    private void ForceWorldRecordLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (worldRecordListParent is RectTransform recordRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(recordRect);
    }

    private void LoadData()
    {
        LoadNpcBook();
        LoadNpcDialogueData();
        LoadWorldRecordData();
        loaded = true;
    }

    private void LoadNpcBook()
    {
        TextAsset json = Resources.Load<TextAsset>(npcBookResourcePath);
        if (json == null)
        {
            Debug.LogWarning("[DoGamRecord] Missing Resources/" + npcBookResourcePath + ".json", this);
            npcBookItems = new List<RecordNPCBookItemData>();
            return;
        }

        try
        {
            RecordNPCBookData data =
                JsonConvert.DeserializeObject<RecordNPCBookData>(json.text);

            npcBookItems = data != null && data.items != null
                ? data.items
                    .Where(x => x != null &&
                                !string.IsNullOrWhiteSpace(x.id) &&
                                !string.IsNullOrWhiteSpace(x.dialogueNpcId))
                    .ToList()
                : new List<RecordNPCBookItemData>();
        }
        catch (Exception e)
        {
            Debug.LogError("[DoGamRecord] NPCBook parse failed: " + e.Message, this);
            npcBookItems = new List<RecordNPCBookItemData>();
        }
    }

    private void LoadNpcDialogueData()
    {
        npcDialogueById.Clear();

        TextAsset json = Resources.Load<TextAsset>(npcDialogueResourcePath);
        if (json == null)
        {
            Debug.LogWarning("[DoGamRecord] Missing Resources/" + npcDialogueResourcePath + ".json", this);
            return;
        }

        try
        {
            NPCDialogueDataList data =
                JsonConvert.DeserializeObject<NPCDialogueDataList>(json.text);

            if (data == null || data.npcs == null)
                return;

            foreach (NPCDialogueData npc in data.npcs)
            {
                if (npc == null || string.IsNullOrWhiteSpace(npc.npcId))
                    continue;

                npcDialogueById[npc.npcId] = npc;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[DoGamRecord] NPCDialogueData parse failed: " + e.Message, this);
        }
    }

    private void LoadWorldRecordData()
    {
        TextAsset json = Resources.Load<TextAsset>(worldRecordResourcePath);
        if (json == null)
        {
            Debug.LogWarning(
                "[DoGamRecord] Resources/" + worldRecordResourcePath +
                ".json 이 아직 없습니다. 마을 기록 서브탭은 빈 상태로 표시됩니다.",
                this);
            worldRecords = new List<WorldRecordItemData>();
            return;
        }

        try
        {
            WorldRecordBookData data =
                JsonConvert.DeserializeObject<WorldRecordBookData>(json.text);

            worldRecords = data != null && data.records != null
                ? data.records.Where(x => x != null && !string.IsNullOrWhiteSpace(x.id)).ToList()
                : new List<WorldRecordItemData>();
        }
        catch (Exception e)
        {
            Debug.LogError("[DoGamRecord] WorldRecordData parse failed: " + e.Message, this);
            worldRecords = new List<WorldRecordItemData>();
        }
    }

    private void BuildNpcGrid()
    {
        ClearChildren(npcGridParent);
        npcViews.Clear();

        foreach (RecordNPCBookItemData item in npcBookItems)
        {
            RecordNPCBookItemData captured = item;

            GameObject instance = Instantiate(npcItemPrefab, npcGridParent);
            instance.name = "RecordNPC_" + item.id;
            instance.SetActive(true);

            Button button = instance.GetComponentInChildren<Button>(true);
            Image icon = FindNamedImage(instance, "Icon", button);

            if (button == null || icon == null)
            {
                Debug.LogError(
                    "[DoGamRecord] npcItemPrefab에는 Button과 Icon(Image)이 필요합니다.",
                    instance);
                Destroy(instance);
                continue;
            }

            icon.sprite = LoadNpcSprite(item.image);
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectNpc(captured, true));

            npcViews.Add(new NPCButtonView
            {
                data = item,
                button = button,
                icon = icon
            });

            EnsureButtonHoverDoesNotUseIcon(button, icon);
            registerHover?.Invoke(button);
        }

        npcGridBuilt = true;
        RefreshNpcButtonColors();

        if (npcGridParent is RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }

    private void SelectNpc(RecordNPCBookItemData item, bool playSound)
    {
        selectedNpc = item;

        if (playSound)
            PlayPageSound();

        RefreshNpcButtonColors();
        RenderSelectedNpc();
    }

    private void RenderSelectedNpc()
    {
        ClearChildren(dialogueListParent);
        ShowDialogueList();

        if (selectedNpc == null)
        {
            ClearNpcRightPage();
            return;
        }

        bool hasMet = HasMetNpc(selectedNpc);
        NPCDialogueData dialogueData = GetNpcDialogueData(selectedNpc.dialogueNpcId);

        if (npcDetailImage != null)
        {
            npcDetailImage.sprite = LoadNpcSprite(selectedNpc.image);
            npcDetailImage.enabled = npcDetailImage.sprite != null;
            npcDetailImage.preserveAspect = true;

            ApplyDiscoveryVisual(
                npcDetailImage,
                hasMet,
                Color.white
            );
        }

        if (!hasMet)
        {
            if (npcDetailNameText != null) npcDetailNameText.text = "????";
            if (npcProgressText != null) npcProgressText.text = lockedNpcText;
            return;
        }

        if (npcDetailNameText != null)
            npcDetailNameText.text = GetLocalizedNpcName(dialogueData, selectedNpc);

        if (dialogueData == null || dialogueData.dialogueSets == null)
        {
            if (npcProgressText != null) npcProgressText.text = "이야기 0 / 0";
            return;
        }

        // 기존 진행 데이터에 남아 있는 seenSetIds를 영구 도감 기록으로 흡수.
        if (NPCDialogueProgressManager.Instance != null)
        {
            NPCDialogueNpcProgressData progress =
                NPCDialogueProgressManager.Instance.GetOrCreateNpcProgress(selectedNpc.dialogueNpcId);

            DoGamRecordProgress.ImportNpcDialogueProgress(
                selectedNpc.dialogueNpcId,
                progress);
        }

        int treeLevel = GetCurrentTreeLevel();

        List<NPCDialogueSetData> availableSets =
     dialogueData.dialogueSets
         .Where(x =>
             x != null &&
             (!x.useTreeLevel || x.treeLevel <= treeLevel))
         .OrderBy(x =>
             x.useTreeLevel ? x.treeLevel : 0)
         .ThenBy(x =>
             dialogueData.dialogueSets.IndexOf(x))
         .ToList();

        int discoveredCount = availableSets.Count(x =>
            DoGamRecordProgress.IsDialogueDiscovered(
                selectedNpc.dialogueNpcId,
                x.setId));

        if (npcProgressText != null)
            npcProgressText.text = $"이야기 {discoveredCount} / {availableSets.Count}";

        int lastLevel = int.MinValue;

        int? previousLevel = null;

        foreach (NPCDialogueSetData set in availableSets)
        {
            int currentLevel =
                set.useTreeLevel
                    ? set.treeLevel
                    : 0;

            // 이전 단계와 달라졌다면
            // 다음 단계 시작 전에 구분 이미지를 넣는다.
            if (previousLevel.HasValue &&
                previousLevel.Value != currentLevel)
            {
                CreateDialogueStageDivider();
            }

            bool discovered =
                DoGamRecordProgress.IsDialogueDiscovered(
                    selectedNpc.dialogueNpcId,
                    set.setId
                );

            CreateDialogueListItem(
                dialogueData,
                set,
                discovered
            );

            previousLevel = currentLevel;
        }
    }

    private void CreateDialogueStageDivider()
    {
        if (dialogueStageDividerPrefab == null ||
            dialogueListParent == null)
        {
            return;
        }

        GameObject divider =
            Instantiate(
                dialogueStageDividerPrefab,
                dialogueListParent
            );

        divider.name = "DialogueStageDivider";
        divider.SetActive(true);
    }

    private void CreateDialogueListItem(
    NPCDialogueData npcData,
    NPCDialogueSetData set,
    bool discovered)
    {
        GameObject instance =
            Instantiate(dialogueItemPrefab, dialogueListParent);

        instance.name = "DialogueRecord_" + set.setId;
        instance.SetActive(true);

        TMP_Text titleText = FindNamedText(instance, "Title");

        if (titleText == null)
            titleText = instance.GetComponentInChildren<TMP_Text>(true);

        if (titleText != null)
        {
            titleText.text = discovered
                ? GetLocalizedDialogueTitle(npcData, set)
                : lockedDialogueTitle;
        }

        // 이 항목 안의 실제 Button들을 모두 가져온다.
        Button[] buttons =
            instance.GetComponentsInChildren<Button>(true);

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError(
                $"[DoGamRecord] DialogueItem에 Button이 없습니다. setId={set.setId}",
                instance);

            return;
        }

        Debug.Log(
            $"[DoGamRecord] DialogueItem 생성 / " +
            $"setId={set.setId}, buttonCount={buttons.Length}",
            instance);

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(() =>
            {
                Debug.Log(
                    $"[DoGamRecord] Dialogue CLICK / " +
                    $"button={button.name}, " +
                    $"setId={set.setId}, " +
                    $"discovered={discovered}",
                    button);

                PlayPageSound();

                if (discovered)
                    ShowDialogueDetail(npcData, set);
                else
                    ShowLockedDialogueDetail();
            });

            registerHover?.Invoke(button);
        }
    }

    private void ShowDialogueDetail(
    NPCDialogueData npcData,
    NPCDialogueSetData set)
    {
        Debug.Log(
            $"[DoGamRecord] ShowDialogueDetail ENTER / setId={set?.setId}",
            this);

        if (dialogueListRoot != null)
            dialogueListRoot.SetActive(false);

        if (dialogueDetailRoot != null)
            dialogueDetailRoot.SetActive(true);
        else
            Debug.LogError(
                "[DoGamRecord] DialogueDetailRoot가 Inspector에 연결되지 않았습니다.",
                this);

        if (dialogueDetailTitleText != null)
            dialogueDetailTitleText.text =
                GetLocalizedDialogueTitle(npcData, set);

        if (dialogueDetailBodyText != null)
            dialogueDetailBodyText.text =
                GetDialoguePreviewText(npcData, set);

        Debug.Log(
            $"[DoGamRecord] Detail Result / " +
            $"List={(dialogueListRoot != null ? dialogueListRoot.activeSelf : false)}, " +
            $"Detail={(dialogueDetailRoot != null ? dialogueDetailRoot.activeSelf : false)}",
            this);
    }

    private void ShowLockedDialogueDetail()
    {
        Debug.Log("[DoGamRecord] ShowLockedDialogueDetail", this);

        if (dialogueListRoot != null)
            dialogueListRoot.SetActive(false);

        if (dialogueDetailRoot != null)
            dialogueDetailRoot.SetActive(true);

        if (dialogueDetailTitleText != null)
            dialogueDetailTitleText.text = lockedDialogueTitle;

        if (dialogueDetailBodyText != null)
            dialogueDetailBodyText.text = lockedDialogueBody;

        Canvas.ForceUpdateCanvases();

        if (dialogueDetailRoot != null &&
            dialogueDetailRoot.transform is RectTransform detailRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailRect);
        }

        Debug.Log(
            $"[DoGamRecord] Locked detail state / " +
            $"list={(dialogueListRoot != null ? dialogueListRoot.activeSelf.ToString() : "NULL")}, " +
            $"detail={(dialogueDetailRoot != null ? dialogueDetailRoot.activeSelf.ToString() : "NULL")}",
            this);
    }

    private void ShowDialogueList()
    {
        if (dialogueListRoot != null)
            dialogueListRoot.SetActive(true);

        if (dialogueDetailRoot != null)
            dialogueDetailRoot.SetActive(false);
    }

    private string GetDialoguePreviewText(
        NPCDialogueData npcData,
        NPCDialogueSetData set)
    {
        if (npcData == null ||
            set == null ||
            npcData.nodes == null ||
            string.IsNullOrWhiteSpace(set.startNodeId))
            return string.Empty;

        NPCDialogueNodeData startNode =
            npcData.nodes.Find(x =>
                x != null &&
                string.Equals(x.nodeId, set.startNodeId, StringComparison.Ordinal));

        if (startNode == null ||
            startNode.lines == null ||
            startNode.lines.Count == 0)
            return string.Empty;

        List<string> lines = new List<string>();

        for (int i = 0; i < startNode.lines.Count; i++)
        {
            string fallback = startNode.lines[i];
            string key =
                $"npc.{npcData.npcId}.node.{startNode.nodeId}.line.{i + 1:00}";

            string value = GetLocalizedText(
                npcDialogueLocalizationTable,
                key,
                fallback);

            lines.Add(ReplacePlayerNameToken(value));
        }

        return string.Join("\n\n", lines);
    }

    private void BuildWorldRecordList()
    {
        ClearChildren(worldRecordListParent);
        worldRecordViews.Clear();

        int treeLevel = GetCurrentTreeLevel();

        List<WorldRecordItemData> visible =
            worldRecords
                .Where(x =>
                    x.requiredTreeLevel < 0 ||
                    x.requiredTreeLevel <= treeLevel)
                .ToList();

        int discoveredCount =
            visible.Count(x =>
                DoGamRecordProgress
                    .IsWorldRecordDiscovered(x.id));

        if (worldRecordProgressText != null)
        {
            worldRecordProgressText.text =
                $"기록 {discoveredCount} / {visible.Count}";
        }

        foreach (WorldRecordItemData record in visible)
        {
            bool discovered =
                DoGamRecordProgress
                    .IsWorldRecordDiscovered(record.id);

            CreateWorldRecordListItem(
                record,
                discovered);
        }

        if (visible.Count == 0)
        {
            selectedWorldRecord = null;
            ClearWorldRecordDetail();
            return;
        }

        // 기록 탭에 들어올 때 항상 첫 번째 항목 선택
        SelectWorldRecord(
            visible[0],
            false
        );
    }

    private void CreateWorldRecordListItem(
    WorldRecordItemData record,
    bool discovered)
    {
        GameObject instance =
            Instantiate(
                worldRecordItemPrefab,
                worldRecordListParent
            );

        instance.name =
            "WorldRecord_" + record.id;

        instance.SetActive(true);

        Button button =
            instance.GetComponentInChildren<Button>(true);

        TMP_Text titleText =
            FindNamedText(instance, "Title");

        Image icon =
            FindNamedImage(
                instance,
                "Icon",
                button
            );

        if (titleText == null)
            titleText =
                instance.GetComponentInChildren<TMP_Text>(true);

        if (titleText != null)
        {
            titleText.text =
                discovered
                    ? GetLocalizedText(
                        doGamLocalizationTable,
                        record.titleKey,
                        record.title)
                    : lockedWorldRecordTitle;
        }

        if (icon != null)
        {
            icon.sprite =
                LoadWorldRecordSprite(record.image);

            icon.enabled =
                icon.sprite != null;

            icon.preserveAspect = true;

            ApplyDiscoveryVisual(
                icon,
                discovered,
                Color.white
            );
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(() =>
            {
                SelectWorldRecord(
                    record,
                    true
                );
            });

            EnsureButtonHoverDoesNotUseIcon(button, icon);
            registerHover?.Invoke(button);
        }

        worldRecordViews.Add(
            new WorldRecordButtonView
            {
                data = record,
                button = button,
                icon = icon,
                discovered = discovered
            }
        );
    }

    private void SelectWorldRecord(
    WorldRecordItemData record,
    bool playSound)
    {
        if (record == null)
            return;

        selectedWorldRecord = record;

        if (playSound)
            PlayPageSound();

        bool discovered =
            DoGamRecordProgress
                .IsWorldRecordDiscovered(record.id);

        RefreshWorldRecordButtonVisuals();

        ShowWorldRecordDetail(
            record,
            discovered
        );
    }

    private void RefreshWorldRecordButtonVisuals()
    {
        foreach (WorldRecordButtonView view
                 in worldRecordViews)
        {
            if (view == null ||
                view.icon == null)
            {
                continue;
            }

            if (!view.discovered)
            {
                ApplyDiscoveryVisual(
                    view.icon,
                    false,
                    Color.white
                );

                continue;
            }

            bool selected =
                ReferenceEquals(
                    view.data,
                    selectedWorldRecord
                );

            ApplyDiscoveryVisual(
                view.icon,
                true,
                selected
                    ? selectedColor
                    : unselectedColor
            );
        }
    }

    private void ShowWorldRecordDetail(
        WorldRecordItemData record,
        bool discovered)
    {
        if (!discovered)
        {
            if (worldRecordDetailTitleText != null)
                worldRecordDetailTitleText.text = lockedWorldRecordTitle;

            if (worldRecordDetailLocationText != null)
                worldRecordDetailLocationText.text = string.Empty;

            if (worldRecordDetailBodyText != null)
                worldRecordDetailBodyText.text = lockedWorldRecordBody;

            if (worldRecordDetailImage != null)
            {
                worldRecordDetailImage.sprite = LoadWorldRecordSprite(record.image);
                worldRecordDetailImage.enabled = worldRecordDetailImage.sprite != null;
                worldRecordDetailImage.preserveAspect = true;

                ApplyDiscoveryVisual(
                    worldRecordDetailImage,
                    false,
                    Color.white
                );
            }

            return;
        }

        if (worldRecordDetailTitleText != null)
        {
            worldRecordDetailTitleText.text =
                GetLocalizedText(
                    doGamLocalizationTable,
                    record.titleKey,
                    record.title);
        }

        if (worldRecordDetailLocationText != null)
        {
            worldRecordDetailLocationText.text =
                GetLocalizedText(
                    doGamLocalizationTable,
                    record.locationKey,
                    record.location);
        }

        if (worldRecordDetailBodyText != null)
        {
            worldRecordDetailBodyText.text =
                GetLocalizedText(
                    doGamLocalizationTable,
                    record.textKey,
                    record.text);
        }

        if (worldRecordDetailImage != null)
        {
            worldRecordDetailImage.sprite = LoadWorldRecordSprite(record.image);
            worldRecordDetailImage.enabled = worldRecordDetailImage.sprite != null;
            worldRecordDetailImage.preserveAspect = true;

            ApplyDiscoveryVisual(
                worldRecordDetailImage,
                true,
                Color.white
            );
        }
    }

    private void ClearNpcRightPage()
    {
        if (npcDetailImage != null)
        {
            npcDetailImage.sprite = null;
            npcDetailImage.enabled = false;
            npcDetailImage.material = null;
            npcDetailImage.color = Color.white;
        }

        if (npcDetailNameText != null) npcDetailNameText.text = string.Empty;
        if (npcProgressText != null) npcProgressText.text = string.Empty;

        ClearChildren(dialogueListParent);
        ShowDialogueList();
    }

    private void ClearWorldRecordDetail()
    {
        if (worldRecordDetailTitleText != null)
            worldRecordDetailTitleText.text = string.Empty;

        if (worldRecordDetailLocationText != null)
            worldRecordDetailLocationText.text = string.Empty;

        if (worldRecordDetailBodyText != null)
            worldRecordDetailBodyText.text = string.Empty;

        if (worldRecordDetailImage != null)
        {
            worldRecordDetailImage.sprite = null;
            worldRecordDetailImage.enabled = false;
            worldRecordDetailImage.material = null;
            worldRecordDetailImage.color = Color.white;
        }
    }

    private void RefreshNpcButtonColors()
    {
        foreach (NPCButtonView view in npcViews)
        {
            if (view == null || view.icon == null)
                continue;

            bool discovered = HasMetNpc(view.data);

            if (!discovered)
            {
                ApplyDiscoveryVisual(
                    view.icon,
                    false,
                    Color.white
                );

                continue;
            }

            Color color =
                ReferenceEquals(view.data, selectedNpc)
                    ? selectedColor
                    : unselectedColor;

            ApplyDiscoveryVisual(
                view.icon,
                true,
                color
            );
        }
    }

    private bool HasMetNpc(RecordNPCBookItemData item)
    {
        return item != null &&
               !string.IsNullOrWhiteSpace(item.dialogueNpcId) &&
               NPCDialogueProgressManager.Instance != null &&
               NPCDialogueProgressManager.Instance.HasMetNpc(item.dialogueNpcId);
    }

    private NPCDialogueData GetNpcDialogueData(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return null;

        npcDialogueById.TryGetValue(npcId, out NPCDialogueData data);
        return data;
    }

    private string GetLocalizedNpcName(
        NPCDialogueData dialogueData,
        RecordNPCBookItemData bookItem)
    {
        string fallback =
            dialogueData != null && !string.IsNullOrWhiteSpace(dialogueData.npcName)
                ? dialogueData.npcName
                : bookItem?.name;

        string key =
            dialogueData != null && !string.IsNullOrWhiteSpace(dialogueData.npcId)
                ? $"npc.{dialogueData.npcId}.name"
                : bookItem?.nameKey;

        return GetLocalizedText(
            npcDialogueLocalizationTable,
            key,
            fallback);
    }

    private string GetLocalizedDialogueTitle(
        NPCDialogueData npcData,
        NPCDialogueSetData set)
    {
        if (npcData == null || set == null)
            return string.Empty;

        // 이 키가 Localization Table에 없으면 JSON title을 그대로 사용한다.
        string key =
            $"npc.{npcData.npcId}.set.{set.setId}.title";

        return GetLocalizedText(
            npcDialogueLocalizationTable,
            key,
            set.title);
    }

    private string GetLocalizedText(
        string table,
        string key,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback ?? string.Empty;

        string localized =
            LocalizationSettings.StringDatabase.GetLocalizedString(
                table,
                key);

        if (string.IsNullOrEmpty(localized) ||
            localized.Contains("No translation found"))
            return fallback ?? string.Empty;

        return localized;
    }

    private string ReplacePlayerNameToken(string text)
    {
        if (NPCDialogueDatabase.Instance != null)
            return NPCDialogueDatabase.Instance.ReplacePlayerNameTokenInText(text);

        return text ?? string.Empty;
    }

    private int GetCurrentTreeLevel()
    {
        return Mathf.Max(0, TreeLevelUnlocker.GetSavedCurrentLevel());
    }

    private void ApplySubTabSprites()
    {
        if (characterSubTabButton != null &&
            characterSubTabButton.image != null &&
            subTabNormalSprite != null)
        {
            characterSubTabButton.image.sprite =
                currentSubTab == SubTab.Character
                    ? subTabPressedSprite
                    : subTabNormalSprite;
        }

        if (worldRecordSubTabButton != null &&
            worldRecordSubTabButton.image != null &&
            subTabNormalSprite != null)
        {
            worldRecordSubTabButton.image.sprite =
                currentSubTab == SubTab.WorldRecord
                    ? subTabPressedSprite
                    : subTabNormalSprite;
        }
    }

    private void OnRecordProgressChanged()
    {
        if (IsOpen)
            RefreshCurrentView();
    }

    private Sprite LoadNpcSprite(string imageName)
    {
        return LoadSprite(
            npcSprites,
            npcSpriteResourceFolder,
            imageName);
    }

    private Sprite LoadWorldRecordSprite(string imageName)
    {
        return LoadSprite(
            recordSprites,
            worldRecordSpriteResourceFolder,
            imageName);
    }

    private static Sprite LoadSprite(
        Dictionary<string, Sprite> cache,
        string folder,
        string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return null;

        if (cache.TryGetValue(imageName, out Sprite cached))
            return cached;

        string path =
            string.IsNullOrWhiteSpace(folder)
                ? imageName
                : folder.TrimEnd('/') + "/" + imageName;

        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
        {
            Sprite[] candidates = Resources.LoadAll<Sprite>(path);
            if (candidates.Length > 0)
                sprite = candidates[0];
        }

        cache[imageName] = sprite;
        return sprite;
    }

    private void EnsureButtonHoverDoesNotUseIcon(
        Button button,
        Image icon)
    {
        if (button == null || icon == null)
            return;

        if (button.targetGraphic != icon)
            return;

        Image buttonBackground = button.GetComponent<Image>();

        if (buttonBackground != null && buttonBackground != icon)
        {
            button.targetGraphic = buttonBackground;
            return;
        }

        foreach (Image image in button.GetComponentsInChildren<Image>(true))
        {
            if (image == null || image == icon)
                continue;

            button.targetGraphic = image;
            return;
        }

        Debug.LogWarning(
            $"[DoGamRecord] Hover target graphic not found for {button.name}.",
            button);
    }
    private static Image FindNamedImage(
        GameObject instance,
        string objectName,
        Button rootButton)
    {
        if (instance == null)
            return null;

        foreach (Image image in instance.GetComponentsInChildren<Image>(true))
        {
            if (image != null &&
                image.name == objectName &&
                (rootButton == null || image.gameObject != rootButton.gameObject))
                return image;
        }

        return null;
    }

    private static TMP_Text FindNamedText(
        GameObject instance,
        string objectName)
    {
        if (instance == null)
            return null;

        foreach (TMP_Text text in instance.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text != null && text.name == objectName)
                return text;
        }

        return null;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; --i)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }

    private static void PlayTabSound()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayBbyongSFX();
    }

    private static void PlayPageSound()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayPageFlipSFX();
    }
}
