using System;
using System.Collections;
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
    public string baseDescription;
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

    [Header("Character - Story Progress UI")]
    [SerializeField] private TextMeshProUGUI npcProgressTitleText;
    [SerializeField] private TextMeshProUGUI npcProgressCountText;
    [SerializeField] private Image npcProgressFillImage;
    [SerializeField] private string npcProgressCountKey = "record.dialogue.progress";
    [SerializeField] private string npcProgressCountFallback = "\uC774\uC57C\uAE30 \uC218\uC9D1\uB3C4";

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

    [Header("Dialogue List / Detail Transition")]
    [SerializeField] private float dialogueTransitionDuration = 0.22f;
    [SerializeField] private float dialogueTransitionDistance = 42f;
    [SerializeField] private float dialogueTransitionMinScale = 0.98f;

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
    private Coroutine dialogueTransitionCoroutine;
    private bool dialogueTransitionBaseCached;
    private Vector2 dialogueListBasePosition;
    private Vector2 dialogueDetailBasePosition;
    private Vector3 dialogueListBaseScale;
    private Vector3 dialogueDetailBaseScale;

    [Header("Scroll Hover Guard")]
    [SerializeField, Range(0f, 1f)]
    private float minimumVisibleRatioForHover = 0.5f;

    [Header("Left Slot Scale Effect")]
    [SerializeField] private float leftSlotNormalScale = 1f;
    [SerializeField] private float leftSlotHoverScale = 1.08f;
    [SerializeField] private float leftSlotSelectedScale = 1.12f;
    [SerializeField] private float leftSlotClickScale = 1f;

    private class ScrollHoverGuard
    {
        public Button button;
        public RectTransform buttonRect;
        public RectTransform viewportRect;
        public CanvasGroup canvasGroup;
        public Graphic hoverGraphic;
        public Material originalMaterial;
        public bool lastAllowed = true;
    }

    private readonly List<ScrollHoverGuard> scrollHoverGuards =
        new List<ScrollHoverGuard>();


    private readonly Dictionary<Button, RectTransform> leftSlotScaleTargets =
        new Dictionary<Button, RectTransform>();
    private readonly Dictionary<Button, Vector3> leftSlotBaseScales =
        new Dictionary<Button, Vector3>();
    private readonly HashSet<Button> leftSlotHovering =
        new HashSet<Button>();

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
            RegisterViewportAwareHover(button, npcGridParent);
            RegisterLeftSlotScaleEffect(
                button,
                instance.transform as RectTransform);
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
            if (npcProgressTitleText != null)
                npcProgressTitleText.text = lockedNpcText;
            ClearNpcProgressUI();
            return;
        }

        if (npcDetailNameText != null)
            npcDetailNameText.text = GetLocalizedNpcName(dialogueData, selectedNpc);

        if (dialogueData == null || dialogueData.dialogueSets == null)
        {
            SetNpcProgressTitle();
            ClearNpcProgressUI();
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

        List<NPCDialogueSetData> allSets =
            dialogueData.dialogueSets
                .Where(x => x != null)
                .OrderBy(x =>
                    x.useTreeLevel ? x.treeLevel : 0)
                .ThenBy(x =>
                    dialogueData.dialogueSets.IndexOf(x))
                .ToList();

        List<NPCDialogueSetData> availableSets =
            allSets
                .Where(x =>
                    !x.useTreeLevel || x.treeLevel <= treeLevel)
                .ToList();

        RefreshNpcProgressUI(allSets);

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

        Transform newIconTransform =
            instance.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(x =>
                    x != null && x.name == "NewIcon");

        GameObject newIcon =
            newIconTransform != null
                ? newIconTransform.gameObject
                : null;

        if (newIcon != null)
        {
            bool isUnread =
                discovered &&
                DoGamRecordProgress.IsDialogueUnread(
                    npcData.npcId,
                    set.setId);

            newIcon.SetActive(isUnread);
        }

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
                {
                    DoGamRecordProgress.MarkDialogueRead(
                        npcData.npcId,
                        set.setId);

                    if (newIcon != null)
                        newIcon.SetActive(false);

                    ShowDialogueDetail(npcData, set);
                }
                else
                {
                    ShowLockedDialogueDetail();
                }
            });

            RegisterViewportAwareHover(button, dialogueListParent);
        }
    }

    private void ShowDialogueDetail(
        NPCDialogueData npcData,
        NPCDialogueSetData set)
    {
        if (dialogueDetailTitleText != null)
            dialogueDetailTitleText.text =
                GetLocalizedDialogueTitle(npcData, set);

        if (dialogueDetailBodyText != null)
            dialogueDetailBodyText.text =
                GetDialoguePreviewText(npcData, set);

        PlayDialoguePanelTransition(true);
    }

    private void ShowLockedDialogueDetail()
    {
        if (dialogueDetailTitleText != null)
            dialogueDetailTitleText.text = lockedDialogueTitle;

        if (dialogueDetailBodyText != null)
            dialogueDetailBodyText.text = lockedDialogueBody;

        PlayDialoguePanelTransition(true);
    }

    private void ShowDialogueList()
    {
        bool detailIsOpen =
            dialogueDetailRoot != null &&
            dialogueDetailRoot.activeSelf;

        if (!detailIsOpen)
        {
            SetDialoguePanelState(false);
            return;
        }

        PlayDialoguePanelTransition(false);
    }

    private void PlayDialoguePanelTransition(bool toDetail)
    {
        CacheDialogueTransitionBaseIfNeeded();

        if (!isActiveAndEnabled)
        {
            RestoreDialogueTransitionBaseTransforms();
            SetDialoguePanelState(toDetail);
            return;
        }

        if (dialogueTransitionCoroutine != null)
        {
            StopCoroutine(dialogueTransitionCoroutine);
            dialogueTransitionCoroutine = null;
            RestoreDialogueTransitionBaseTransforms();
        }

        dialogueTransitionCoroutine =
            StartCoroutine(DialoguePanelTransitionRoutine(toDetail));
    }

    private IEnumerator DialoguePanelTransitionRoutine(bool toDetail)
    {
        GameObject outgoingRoot =
            toDetail ? dialogueListRoot : dialogueDetailRoot;
        GameObject incomingRoot =
            toDetail ? dialogueDetailRoot : dialogueListRoot;

        RectTransform outgoingRect =
            outgoingRoot != null
                ? outgoingRoot.transform as RectTransform
                : null;
        RectTransform incomingRect =
            incomingRoot != null
                ? incomingRoot.transform as RectTransform
                : null;

        if (outgoingRect == null || incomingRect == null)
        {
            SetDialoguePanelState(toDetail);
            dialogueTransitionCoroutine = null;
            yield break;
        }

        CanvasGroup outgoingGroup = GetOrAddCanvasGroup(outgoingRoot);
        CanvasGroup incomingGroup = GetOrAddCanvasGroup(incomingRoot);

        CacheDialogueTransitionBaseIfNeeded();

        Vector2 outgoingBasePos = toDetail
            ? dialogueListBasePosition
            : dialogueDetailBasePosition;
        Vector3 outgoingBaseScale = toDetail
            ? dialogueListBaseScale
            : dialogueDetailBaseScale;
        Vector2 incomingBasePos = toDetail
            ? dialogueDetailBasePosition
            : dialogueListBasePosition;
        Vector3 incomingBaseScale = toDetail
            ? dialogueDetailBaseScale
            : dialogueListBaseScale;

        float direction = toDetail ? 1f : -1f;
        float halfDuration =
            Mathf.Max(0.01f, dialogueTransitionDuration * 0.5f);

        Vector2 outgoingEnd =
            outgoingBasePos +
            new Vector2(-dialogueTransitionDistance * direction, 0f);
        Vector3 outgoingEndScale =
            ScaleFrom(outgoingBaseScale, dialogueTransitionMinScale);

        if (outgoingRoot != null)
            outgoingRoot.SetActive(true);

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            outgoingRect.anchoredPosition =
                Vector2.Lerp(outgoingBasePos, outgoingEnd, eased);
            outgoingRect.localScale =
                Vector3.Lerp(outgoingBaseScale, outgoingEndScale, eased);
            outgoingGroup.alpha = 1f - eased;
            yield return null;
        }

        outgoingRect.anchoredPosition = outgoingBasePos;
        outgoingRect.localScale = outgoingBaseScale;
        outgoingGroup.alpha = 1f;
        outgoingRoot.SetActive(false);

        incomingRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();

        Vector2 incomingStart =
            incomingBasePos +
            new Vector2(dialogueTransitionDistance * direction, 0f);
        Vector3 incomingStartScale =
            ScaleFrom(incomingBaseScale, dialogueTransitionMinScale);

        incomingRect.anchoredPosition = incomingStart;
        incomingRect.localScale = incomingStartScale;
        incomingGroup.alpha = 0f;

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            incomingRect.anchoredPosition =
                Vector2.Lerp(incomingStart, incomingBasePos, eased);
            incomingRect.localScale =
                Vector3.Lerp(incomingStartScale, incomingBaseScale, eased);
            incomingGroup.alpha = eased;
            yield return null;
        }

        Canvas.ForceUpdateCanvases();
        incomingRect.anchoredPosition = incomingBasePos;
        incomingRect.localScale = incomingBaseScale;
        incomingGroup.alpha = 1f;

        dialogueTransitionCoroutine = null;
    }

    private void CacheDialogueTransitionBaseIfNeeded()
    {
        if (dialogueTransitionBaseCached)
            return;

        RectTransform listRect =
            dialogueListRoot != null
                ? dialogueListRoot.transform as RectTransform
                : null;
        RectTransform detailRect =
            dialogueDetailRoot != null
                ? dialogueDetailRoot.transform as RectTransform
                : null;

        if (listRect == null || detailRect == null)
            return;

        // These are the authored positions from the inactive/normal UI state.
        // Never recalculate them from a panel that may already be mid-transition.
        dialogueListBasePosition = listRect.anchoredPosition;
        dialogueDetailBasePosition = detailRect.anchoredPosition;
        dialogueListBaseScale = listRect.localScale;
        dialogueDetailBaseScale = detailRect.localScale;
        dialogueTransitionBaseCached = true;
    }

    private void RestoreDialogueTransitionBaseTransforms()
    {
        if (!dialogueTransitionBaseCached)
            return;

        RestoreDialogueTransitionBase(
            dialogueListRoot,
            dialogueListBasePosition,
            dialogueListBaseScale);
        RestoreDialogueTransitionBase(
            dialogueDetailRoot,
            dialogueDetailBasePosition,
            dialogueDetailBaseScale);
    }

    private static void RestoreDialogueTransitionBase(
        GameObject root,
        Vector2 basePosition,
        Vector3 baseScale)
    {
        if (root == null)
            return;

        RectTransform rect = root.transform as RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = basePosition;
            rect.localScale = baseScale;
        }

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = 1f;
    }

    private void SetDialoguePanelState(bool showDetail)
    {
        CacheDialogueTransitionBaseIfNeeded();
        RestoreDialogueTransitionBaseTransforms();

        if (dialogueListRoot != null)
            dialogueListRoot.SetActive(!showDetail);

        if (dialogueDetailRoot != null)
            dialogueDetailRoot.SetActive(showDetail);

        ResetDialoguePanelTransform(dialogueListRoot);
        ResetDialoguePanelTransform(dialogueDetailRoot);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();
        return group;
    }

    private static void ResetDialoguePanelTransform(GameObject target)
    {
        if (target == null)
            return;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = 1f;
    }

    private static Vector3 ScaleFrom(Vector3 baseScale, float multiplier)
    {
        return new Vector3(
            baseScale.x * multiplier,
            baseScale.y * multiplier,
            baseScale.z);
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
            RegisterViewportAwareHover(button, worldRecordListParent);
            RegisterLeftSlotScaleEffect(
                button,
                instance.transform as RectTransform);
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

        RefreshLeftSlotScaleEffects();
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

    private void SetNpcProgressTitle()
    {
        if (npcProgressTitleText == null)
            return;

        npcProgressTitleText.text =
            selectedNpc != null
                ? (selectedNpc.baseDescription ?? string.Empty)
                : string.Empty;
    }

    private void RefreshNpcProgressUI(
        List<NPCDialogueSetData> allSets)
    {
        SetNpcProgressTitle();

        int totalCount = allSets != null
            ? allSets.Count(x => x != null)
            : 0;

        int discoveredCount = 0;
        if (selectedNpc != null && allSets != null)
        {
            discoveredCount = allSets.Count(set =>
                set != null &&
                DoGamRecordProgress.IsDialogueDiscovered(
                    selectedNpc.dialogueNpcId,
                    set.setId));
        }

        if (npcProgressCountText != null)
        {
            string label = GetLocalizedText(
                doGamLocalizationTable,
                npcProgressCountKey,
                npcProgressCountFallback);

            npcProgressCountText.text =
                $"{label}  {discoveredCount} / {totalCount}";
        }

        if (npcProgressFillImage != null)
        {
            npcProgressFillImage.fillAmount =
                totalCount > 0
                    ? Mathf.Clamp01((float)discoveredCount / totalCount)
                    : 0f;
        }
    }

    private void ClearNpcProgressUI()
    {
        if (npcProgressCountText != null)
            npcProgressCountText.text = string.Empty;

        if (npcProgressFillImage != null)
            npcProgressFillImage.fillAmount = 0f;
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
        if (npcProgressTitleText != null) npcProgressTitleText.text = string.Empty;
        ClearNpcProgressUI();

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

        RefreshLeftSlotScaleEffects();
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

    private void RegisterLeftSlotScaleEffect(
        Button button,
        RectTransform scaleTarget)
    {
        if (button == null || scaleTarget == null)
            return;

        leftSlotScaleTargets[button] = scaleTarget;

        if (!leftSlotBaseScales.ContainsKey(button))
            leftSlotBaseScales[button] = scaleTarget.localScale;

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        AddLeftSlotScaleTrigger(
            trigger,
            EventTriggerType.PointerEnter,
            _ =>
            {
                if (!button.interactable)
                    return;

                leftSlotHovering.Add(button);
                ApplyLeftSlotScaleState(button);
            });

        AddLeftSlotScaleTrigger(
            trigger,
            EventTriggerType.PointerExit,
            _ =>
            {
                leftSlotHovering.Remove(button);
                ApplyLeftSlotScaleState(button);
            });

        AddLeftSlotScaleTrigger(
            trigger,
            EventTriggerType.PointerDown,
            _ =>
            {
                SetLeftSlotScale(button, leftSlotClickScale);
            });

        AddLeftSlotScaleTrigger(
            trigger,
            EventTriggerType.PointerUp,
            _ =>
            {
                ApplyLeftSlotScaleState(button);
            });

        ApplyLeftSlotScaleState(button);
    }

    private static void AddLeftSlotScaleTrigger(
        EventTrigger trigger,
        EventTriggerType eventType,
        UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        if (trigger == null || action == null)
            return;

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };

        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void RefreshLeftSlotScaleEffects()
    {
        List<Button> deadButtons = null;

        foreach (KeyValuePair<Button, RectTransform> pair
                 in leftSlotScaleTargets)
        {
            Button button = pair.Key;
            RectTransform target = pair.Value;

            if (button == null || target == null)
            {
                if (deadButtons == null)
                    deadButtons = new List<Button>();

                deadButtons.Add(button);
                continue;
            }

            ApplyLeftSlotScaleState(button);
        }

        if (deadButtons == null)
            return;

        foreach (Button button in deadButtons)
        {
            leftSlotScaleTargets.Remove(button);
            leftSlotBaseScales.Remove(button);
            leftSlotHovering.Remove(button);
        }
    }

    private void ApplyLeftSlotScaleState(Button button)
    {
        if (button == null)
            return;

        float targetScale = leftSlotNormalScale;

        if (IsLeftSlotSelected(button))
            targetScale = leftSlotSelectedScale;
        else if (leftSlotHovering.Contains(button))
            targetScale = leftSlotHoverScale;

        SetLeftSlotScale(button, targetScale);
    }

    private void SetLeftSlotScale(
        Button button,
        float scaleMultiplier)
    {
        if (button == null ||
            !leftSlotScaleTargets.TryGetValue(
                button,
                out RectTransform target) ||
            target == null)
        {
            return;
        }

        Vector3 baseScale = Vector3.one;
        if (leftSlotBaseScales.TryGetValue(button, out Vector3 savedScale))
            baseScale = savedScale;

        target.localScale = baseScale * scaleMultiplier;
    }

    private bool IsLeftSlotSelected(Button button)
    {
        if (button == null)
            return false;

        foreach (NPCButtonView view in npcViews)
        {
            if (view != null &&
                view.button == button &&
                ReferenceEquals(view.data, selectedNpc))
            {
                return true;
            }
        }

        foreach (WorldRecordButtonView view in worldRecordViews)
        {
            if (view != null &&
                view.button == button &&
                ReferenceEquals(view.data, selectedWorldRecord))
            {
                return true;
            }
        }

        return false;
    }

    private void RegisterViewportAwareHover(
        Button button,
        Transform listParent)
    {
        if (button == null)
            return;

        RectTransform viewportRect = FindScrollViewport(listParent);

        if (viewportRect == null)
        {
            registerHover?.Invoke(button);
            return;
        }

        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect == null)
        {
            registerHover?.Invoke(button);
            return;
        }

        Graphic hoverGraphic = button.targetGraphic != null
            ? button.targetGraphic
            : button.GetComponent<Graphic>();

        Material originalMaterial =
            hoverGraphic != null ? hoverGraphic.material : null;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();

        ScrollHoverGuard guard = new ScrollHoverGuard
        {
            button = button,
            buttonRect = buttonRect,
            viewportRect = viewportRect,
            canvasGroup = canvasGroup,
            hoverGraphic = hoverGraphic,
            originalMaterial = originalMaterial,
            lastAllowed = true
        };

        scrollHoverGuards.Add(guard);

        UpdateScrollHoverGuard(guard, true);
        registerHover?.Invoke(button);
    }

    private RectTransform FindScrollViewport(Transform listParent)
    {
        if (listParent == null)
            return null;

        ScrollRect scrollRect =
            listParent.GetComponentInParent<ScrollRect>(true);

        if (scrollRect == null)
            return null;

        if (scrollRect.viewport != null)
            return scrollRect.viewport;

        return scrollRect.transform as RectTransform;
    }

    private void Update()
    {
        for (int i = scrollHoverGuards.Count - 1; i >= 0; i--)
        {
            ScrollHoverGuard guard = scrollHoverGuards[i];

            if (guard == null ||
                guard.button == null ||
                guard.buttonRect == null ||
                guard.viewportRect == null ||
                guard.canvasGroup == null)
            {
                scrollHoverGuards.RemoveAt(i);
                continue;
            }

            if (!guard.button.gameObject.activeInHierarchy)
                continue;

            UpdateScrollHoverGuard(guard, false);
        }
    }

    private void UpdateScrollHoverGuard(
        ScrollHoverGuard guard,
        bool force)
    {
        if (guard == null ||
            guard.buttonRect == null ||
            guard.viewportRect == null ||
            guard.canvasGroup == null)
        {
            return;
        }

        float visibleRatio =
            GetVisibleAreaRatio(
                guard.buttonRect,
                guard.viewportRect
            );

        bool allowed =
            visibleRatio >= minimumVisibleRatioForHover;

        if (!force && allowed == guard.lastAllowed)
            return;

        guard.lastAllowed = allowed;
        guard.canvasGroup.blocksRaycasts = allowed;

        if (!allowed)
        {
            leftSlotHovering.Remove(guard.button);
            ApplyLeftSlotScaleState(guard.button);

            if (guard.hoverGraphic != null)
            {
                guard.hoverGraphic.material =
                    guard.originalMaterial;
            }
        }
    }

    private static float GetVisibleAreaRatio(
        RectTransform target,
        RectTransform viewport)
    {
        if (target == null || viewport == null)
            return 1f;

        Vector3[] targetCorners = new Vector3[4];
        Vector3[] viewportCorners = new Vector3[4];

        target.GetWorldCorners(targetCorners);
        viewport.GetWorldCorners(viewportCorners);

        float targetMinX = targetCorners[0].x;
        float targetMaxX = targetCorners[2].x;
        float targetMinY = targetCorners[0].y;
        float targetMaxY = targetCorners[2].y;

        float viewportMinX = viewportCorners[0].x;
        float viewportMaxX = viewportCorners[2].x;
        float viewportMinY = viewportCorners[0].y;
        float viewportMaxY = viewportCorners[2].y;

        float targetWidth =
            Mathf.Max(0f, targetMaxX - targetMinX);

        float targetHeight =
            Mathf.Max(0f, targetMaxY - targetMinY);

        float targetArea = targetWidth * targetHeight;

        if (targetArea <= 0.0001f)
            return 0f;

        float overlapWidth =
            Mathf.Max(
                0f,
                Mathf.Min(targetMaxX, viewportMaxX) -
                Mathf.Max(targetMinX, viewportMinX)
            );

        float overlapHeight =
            Mathf.Max(
                0f,
                Mathf.Min(targetMaxY, viewportMaxY) -
                Mathf.Max(targetMinY, viewportMinY)
            );

        return Mathf.Clamp01(
            (overlapWidth * overlapHeight) / targetArea
        );
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
