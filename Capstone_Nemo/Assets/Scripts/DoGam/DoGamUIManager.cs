using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using System.IO;

public class DoGamUIManager : MonoBehaviour
{
    public static DoGamUIManager Instance;

    [Header("Unlock Filter")]
    [Tooltip("잠긴 레시피는 리스트에서 숨길지 여부")]
    [SerializeField] private bool hideLockedRecipes = true;

    [Header("Common")]
    public GameObject panel;
    public Button openButton;
    public Button closeButton;

    [Header("Recipe Nav")]
    public Button nextButton;     // 레시피 엔트리 Next
    public Button prevButton;     // 레시피 엔트리 Prev

    [Header("Category Buttons (Recipe Tab)")]
    public Button tteokButton;
    public Button drinkButton;

    [Header("잠금 오버레이")]
    [SerializeField] private GameObject lockCoverPanel;   // 도감 위를 가리는 패널(자물쇠, 블러 등)

    [Header("Tab Sprites")]
    public Sprite tabNormalSprite;
    public Sprite tabPressedSprite;

    private Button _activeTabButton = null; // 현재 선택된 탭

    //마지막으로 본 카테고리 기억용
    private string currentRecipeCategory = "떡";

    [Header("알림 아이콘")]
    [Tooltip("도감 열기 버튼 위 느낌표 아이콘")]
    public GameObject dogamAlertIcon;     // 도감 버튼용

    [Tooltip("레시피 페이지 우측 상단 등 'NEW' 아이콘")]
    public GameObject recipeAlertIcon;    // 현재 페이지용

    // ======= 도감 새 레시피 알림 상태 =======
    // finishKey 기준 (예: Baekseolgi_finish)
    private HashSet<string> _seenFinishKeys = new();
    private HashSet<string> _unseenFinishKeys = new();
    private string _seenKeyPlayerPrefKey;

    // =============== [레시피 탭 레이아웃] ===============
    [Header("Recipe (레시피)")]
    public GameObject recipeRoot;         // 레시피 전용 루트
    public ScrollRect scrollRect;

    public Image itemImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI recipeText;

    public Transform recipeContentParent;
    public GameObject recipeImagePrefab;  // 작은 아이콘 프리팹
    public GameObject recipeLineBackgroundPrefab;

    // 기존 리스트/인덱스
    private int currentIndex = 0;               // (legacy) entryList 인덱스 기반
    private List<DoGamEntry> entryList = new(); // 현재 표시 리스트(보통 해금만)
    private List<DoGamEntry> allEntries = new(); // 전체 리스트
    private Dictionary<string, DoGamEntry> doGamDict;

    // 잠금 오버레이/페이지 운용용 내부 리스트
    private List<DoGamEntry> _allInCat = new();       // 해당 카테고리의 전체
    private List<DoGamEntry> _unlockedInCat = new();  // 해당 카테고리에서 해금된 것들
    private int _unlockedCount = 0;                   // 해금 개수
    private int _currentIndex = 0;                    // 0.._unlockedCount (마지막+1 = 잠금 첫 페이지)

    private bool _suppressLockOnce = false;

    private bool _hasLockedPeek = false;  // 잠금 페이지(오버레이) 미리보기 존재 여부
    private int _maxIndex = 0;            // 이동 가능한 마지막 인덱스

    // 추가: 다과 팔았을때 보상 정보 UI
    [Header("Recipe Reward Info")]
    public GameObject rewardInfoRoot;       // 전체 한 줄 루트
    public Image rewardCurrencyIcon;        // 재화 아이콘
    public TextMeshProUGUI rewardCurrencyText;
    public Image rewardExpIcon;             // 경험치 아이콘
    public TextMeshProUGUI rewardExpText;

    // =============== [게임방법 탭 레이아웃] ===============
    [Header("How-To (게임 방법)")]
    public GameObject howToRoot;              // 게임방법 전용 루트(전체를 On/Off)
    public Button howToButton;                // 상단 "게임 방법" 열기 버튼
    public Button howToPrevButton;            // 게임방법 스프레드 이전
    public Button howToNextButton;            // 게임방법 스프레드 다음
    public GameObject SubTitle;

    [Tooltip("스프레드(두 페이지) 공통 제목 텍스트 (예: '기본 조작')")]
    public TextMeshProUGUI howToTitleText;

    [Tooltip("왼쪽 페이지에 2개 항목이 그려질 부모 컨테이너")]
    public Transform howToLeftPageParent;

    [Tooltip("오른쪽 페이지에 2개 항목이 그려질 부모 컨테이너")]
    public Transform howToRightPageParent;

    [Tooltip("게임방법 항목 카드 프리팹 (자식: Icon(Image), Body(TMP))")]
    public GameObject howToItemPrefab;

    [Tooltip("스프레드당 항목 수(왼쪽 2 + 오른쪽 2 = 4 고정)")]
    public int howToItemsPerSpread = 4;

    // -------- JSON 데이터 구조(게임방법) --------
    [System.Serializable]
    public class HowToItemData
    {
        public string text;   // 항목 설명
        public string image;  // Resources/Sprites/Guide/{image}
    }

    [System.Serializable]
    public class HowToPageData
    {
        public string title;          // 스프레드(두 페이지) 주제(예: 기본조작)
        public List<HowToItemData> items; // 반드시 4개(왼쪽 2 + 오른쪽 2) 권장
    }

    [System.Serializable]
    public class HowToBookData
    {
        public List<HowToPageData> pages;
    }

    private List<HowToPageData> howToPages = new();
    private int howToSpreadIndex = 0;
    private bool isHowToOpen = false;

    // =============== [제작대정보 탭 레이아웃] ===============
    [Header("Workbench (제작대 정보)")]
    public GameObject makerRoot;          // 제작대 탭 루트
    public Button makerButton;            // 상단 "제작대 정보" 탭 버튼
    public Button makerPrevButton;        // 스프레드 이전
    public Button makerNextButton;        // 스프레드 다음



    [Tooltip("왼쪽 페이지(2개) 부모")]
    public Transform makerLeftPageParent;

    [Tooltip("오른쪽 페이지(2개) 부모")]
    public Transform makerRightPageParent;

    [Tooltip("카드 프리팹(Icon(Image), Body(TMP)) - HowTo용과 동일 프리팹 재사용 가능")]
    public GameObject makerItemPrefab;

    [Tooltip("스프레드당 항목 수(왼쪽 2 + 오른쪽 2 = 4)")]
    public int makerItemsPerSpread = 4;

    [System.Serializable]
    public class MakerItemData
    {
        public string name;    // 제작기 이름
        public string image;   // Resources/Sprites/Guide/{image}
        public string desc;    // 제작기 설명
    }

    [System.Serializable]
    public class MakerBookData
    {
        public List<MakerItemData> items; // 평면 리스트(타이틀 없음)
    }

    // 멤버 변수
    private List<MakerItemData> makerItems = new();
    private int makerSpreadIndex = 0;
    private List<HowToPageData> makerPages = new();

    // ===================== 초기화 =====================
    private void Awake()
    {
        if (Instance == null) Instance = this;

        // 데이터 먼저 로드
        LoadDoGamDataFromJSON();  // 레시피
        LoadHowToFromJSON();      // 게임방법

        // 열기/닫기
        //openButton.onClick.AddListener(() => OpenDoGam("백설기"));
        //closeButton.onClick.AddListener(CloseDoGam);
        openButton.onClick.AddListener(OnOpenButtonClicked);
        closeButton.onClick.AddListener(CloseDoGam);

        // 레시피 카테고리
        //tteokButton.onClick.AddListener(() => FilterByCategory("떡"));
        //drinkButton.onClick.AddListener(() => FilterByCategory("음료"));
        //guestButton.onClick.AddListener(() => FilterByCategory("손님"));
        tteokButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
            FilterByCategory("떡"); });
        drinkButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
            FilterByCategory("음료"); });

        // 레시피 네비
        nextButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayPageFlipSFX();
            NextEntry();
        });

        prevButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayPageFlipSFX();
            PrevEntry();
        });


        // 게임방법 열기 / 네비
        if (howToButton != null) howToButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
            OpenHowToTab();
        });
        if (howToNextButton != null) howToNextButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayPageFlipSFX();
            ChangeHowToSpread(+1);
        });
        if (howToPrevButton != null) howToPrevButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayPageFlipSFX();
            ChangeHowToSpread(-1);
        });


        // 제작대 정보 열기 / 네비
        if (makerButton != null) makerButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayBbyongSFX();
            OpenMakerTab();
        });
        if (makerNextButton != null) makerNextButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayPageFlipSFX();
            ChangeMakerSpread(+1);
        });
        if (makerPrevButton != null) makerPrevButton.onClick.AddListener(() => {
            if (SFXManager.Instance) SFXManager.Instance.PlayPageFlipSFX();
            ChangeMakerSpread(-1);
        });



        // 초기 표시 상태
        panel.SetActive(false);
        SubTitle.SetActive(false);
        SetRecipeNavVisible(false);
        SetHowToNavVisible(false);
        SetMakerNavVisible(false);
        if (howToRoot != null) howToRoot.SetActive(false); // 시작 시 비활성
        if (lockCoverPanel) lockCoverPanel.SetActive(false);
        if (makerRoot != null) makerRoot.SetActive(false);

        InitSeenRecipeState();
    }

    // ===================== 공통 토글 =====================
    private void SetRecipeLayout(bool on)
    {
        if (recipeRoot != null) recipeRoot.SetActive(on);
        SetRecipeNavVisible(on && (entryList.Count > 0 || _unlockedCount >= 0));
    }
    private void SetHowToLayout(bool on)
    {
        if (howToRoot != null) howToRoot.SetActive(on);
        SetHowToNavVisible(on && howToPages.Count > 0);

        // 게임방법 탭이 켜질 땐 잠금패널은 항상 꺼짐
        if (on && lockCoverPanel) lockCoverPanel.SetActive(false);
    }
    private void SetRecipeNavVisible(bool on)
    {
        if (!on)
        {
            // 탭 자체를 끌 때는 둘 다 완전히 숨김
            if (prevButton != null) prevButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
        }
        else
        {

            // 현재 페이지 상태 기준으로 보이기/숨기기 다시 계산
            UpdatePage();
        }
    }
    private void SetHowToNavVisible(bool on)
    {
        if (!on)
        {
            if (howToPrevButton != null) howToPrevButton.gameObject.SetActive(false);
            if (howToNextButton != null) howToNextButton.gameObject.SetActive(false);
        }
        else
        {
            // 켤 때는 현재 스프레드 기준으로 버튼 보이기/숨기기 재계산
            RenderHowToSpread();
        }
    }

    // ===================== 도감 열기/닫기 =====================
    public void OpenDoGam(string itemName)
    {
        // 새로 해금된 레시피가 있다면 갱신
        RefreshUnseenFinishKeys();

        // 씨앗 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;

        // 재고 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen())
            return;

        // 가게 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
            return;

        // 가루 변환 패널 열려 있으면 도감 오픈 막기
        if (MillManager.Instance != null && MillManager.Instance.IsOpen())
            return;

        // 상점 패널 열려 있으면 도감 오픈 막기
        if (ShopManager.Instance != null && ShopManager.Instance.IsOpen())
            return;

        if (doGamDict == null || !doGamDict.ContainsKey(itemName))
        {
            Debug.LogWarning($"도감 항목 '{itemName}'을 찾을 수 없습니다.");
            return;
        }

        FilterByCategory("떡"); // 기본은 레시피 탭의 '떡'으로
        SetActiveTab(tteokButton);

        // 버튼을 가장 위로 (레이캐스트 우선순위)
        prevButton.transform.SetAsLastSibling();
        nextButton.transform.SetAsLastSibling();

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayDogamOpenSFX();

        var entry = doGamDict[itemName];
        panel.SetActive(true);

        // 레이아웃: 레시피 탭 On / 게임방법 탭 Off
        SetRecipeLayout(true);
        SetHowToLayout(false);
        SetMakerLayout(false);
        SubTitle.SetActive(false);
        isHowToOpen = false;

        openButton.interactable = false;

        nameText.text = entry.name;
        descriptionText.text = entry.description;
        recipeText.text = string.Join("\n", entry.recipe);
        itemImage.sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + entry.image);

        if (lockCoverPanel) lockCoverPanel.SetActive(false);
    }

    // 도감 버튼 눌렀을 때 진입 지점
    private void OnOpenButtonClicked()
    {
        //튜토리얼 진행 트리거 7
        if (StoreTutorialManager.Instance && StoreTutorialManager.Instance.IsCurrentStep(StoreTutorialStep.NextOrder))
        {
            StoreTutorialManager.Instance.GoToNextStep();
        }

        // 새로 해금된 레시피가 있다면 갱신
        RefreshUnseenFinishKeys();

        // 현재 선택된 세이브 기준 키
        string lastTabKey = GetDoGamStateKey("DoGam_LastTab");

        // 세이브 파일에 기록된 마지막 페이지가 없으면 = 첫 진입
        if (!PlayerPrefs.HasKey(lastTabKey))
        {
            // 게임방법 페이지로 오픈
            OpenHowToTab();
        }
        else
        {
            // 저장된 마지막 상태로 오픈
            OpenDoGamRestoreLastState();
        }
    }

    private void OpenDoGamRestoreLastState()
    {
        // 인벤토리/상점 등 열려 있으면 도감 막기 (기존 OpenDoGam과 동일 로직 복붙)
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;
        if (StorageInventoryUIManager.Instance != null && StorageInventoryUIManager.Instance.IsOpen())
            return;
        if (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
            return;
        if (MillManager.Instance != null && MillManager.Instance.IsOpen())
            return;
        if (ShopManager.Instance != null && ShopManager.Instance.IsOpen())
            return;

        panel.SetActive(true);
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayDogamOpenSFX();

        openButton.interactable = false;
        if (lockCoverPanel) lockCoverPanel.SetActive(false);

        int lastTab = PlayerPrefs.GetInt(GetDoGamStateKey("DoGam_LastTab"), 0);

        // 0: 레시피, 1: 게임방법, 2: 제작대
        if (lastTab == 1)
        {
            // 게임방법 탭으로 복원
            SetRecipeLayout(false);
            SetMakerLayout(false);
            SetHowToLayout(true);
            isHowToOpen = true;
            SubTitle.SetActive(true);
            SetActiveTab(howToButton);

            if (howToPages == null || howToPages.Count == 0)
                LoadHowToFromJSON();

            howToSpreadIndex = PlayerPrefs.GetInt(GetDoGamStateKey("DoGam_LastHowToSpread"), 0);
            howToSpreadIndex = Mathf.Clamp(howToSpreadIndex, 0,
                (howToPages != null && howToPages.Count > 0) ? howToPages.Count - 1 : 0);

            RenderHowToSpread();
        }
        else if (lastTab == 2)
        {
            // 제작대 탭으로 복원
            SetRecipeLayout(false);
            SetHowToLayout(false);
            SubTitle.SetActive(false);

            if (makerItems == null || makerItems.Count == 0)
                LoadMakerFromJSON();

            SetMakerLayout(true);
            SetActiveTab(makerButton);

            int spreadCount = Mathf.CeilToInt(
                (makerItems != null ? (float)makerItems.Count : 0f) / makerItemsPerSpread
            );
            makerSpreadIndex = PlayerPrefs.GetInt(GetDoGamStateKey("DoGam_LastMakerSpread"), 0);
            makerSpreadIndex = Mathf.Clamp(makerSpreadIndex, 0, Mathf.Max(0, spreadCount - 1));

            RenderMakerSpread();
        }
        else
        {
            // 레시피 탭으로 복원
            SubTitle.SetActive(false);
            isHowToOpen = false;

            string cat = PlayerPrefs.GetString(GetDoGamStateKey("DoGam_LastCategory"), "떡");
            int savedIndex = PlayerPrefs.GetInt(GetDoGamStateKey("DoGam_LastUnlockedIndex"), 0);

            // 카테고리 필터 먼저 적용
            FilterByCategory(cat);

            // FilterByCategory에서 _currentIndex가 0으로 초기화되므로, 다시 세팅 후 페이지 갱신
            _currentIndex = savedIndex;
            UpdatePage();
        }
    }


    public bool IsOpen()
    {
        return panel != null && panel.activeSelf;
    }


public void CloseDoGam()
    {
        //튜토리얼 진행 트리거 8
        if (StoreTutorialManager.Instance && StoreTutorialManager.Instance.IsCurrentStep(StoreTutorialStep.DogamCheck))
        {
            StoreTutorialManager.Instance.GoToNextStep();
        }

        // 현재 보고 있던 페이지 상태 저장
        SaveLastDoGamState();

        SFXManager.Instance.PlayBbyongSFX();
        panel.SetActive(false);
        SubTitle.SetActive(false);

        SetRecipeNavVisible(false);
        SetHowToNavVisible(false);

        if (openButton != null) openButton.interactable = true;
        isHowToOpen = false;

        if (lockCoverPanel) lockCoverPanel.SetActive(false);

        SetMakerNavVisible(false);
    }

    private void SaveLastDoGamState()
    {
        // 어떤 탭이 열려 있었는지
        int tab = 0; // 0: 레시피
        if (howToRoot != null && howToRoot.activeSelf) tab = 1;
        else if (makerRoot != null && makerRoot.activeSelf) tab = 2;

        // 세이브별로 분리된 키 사용
        PlayerPrefs.SetInt(GetDoGamStateKey("DoGam_LastTab"), tab);

        if (tab == 0)
        {
            // 레시피 탭: 카테고리 + 현재 페이지 인덱스
            PlayerPrefs.SetString(GetDoGamStateKey("DoGam_LastCategory"), currentRecipeCategory);
            PlayerPrefs.SetInt(GetDoGamStateKey("DoGam_LastUnlockedIndex"), _currentIndex);
        }
        else if (tab == 1)
        {
            PlayerPrefs.SetInt(GetDoGamStateKey("DoGam_LastHowToSpread"), howToSpreadIndex);
        }
        else if (tab == 2)
        {
            PlayerPrefs.SetInt(GetDoGamStateKey("DoGam_LastMakerSpread"), makerSpreadIndex);
        }

        PlayerPrefs.Save();
    }

    // =============== [알림 상태 유틸] ===============

    private string GetSeenPrefKey()
    {
        if (!string.IsNullOrEmpty(_seenKeyPlayerPrefKey))
            return _seenKeyPlayerPrefKey;

        // 세이브 슬롯별로 분리
        string server = PlayerPrefs.GetString("SelectedSave", "");
        _seenKeyPlayerPrefKey = string.IsNullOrEmpty(server)
            ? "DoGam_SeenRecipes"
            : server + ":DoGam_SeenRecipes";

        return _seenKeyPlayerPrefKey;
    }

    private void InitSeenRecipeState()
    {
        LoadSeenFinishKeys();
        RefreshUnseenFinishKeys();
    }

    private void LoadSeenFinishKeys()
    {
        _seenFinishKeys.Clear();
        string key = GetSeenPrefKey();
        string raw = PlayerPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(raw)) return;

        foreach (var token in raw.Split(','))
        {
            var t = token.Trim();
            if (!string.IsNullOrEmpty(t))
                _seenFinishKeys.Add(t);
        }
    }

    private void SaveSeenFinishKeys()
    {
        string key = GetSeenPrefKey();
        string raw = string.Join(",", _seenFinishKeys);
        PlayerPrefs.SetString(key, raw);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// UnlockManager 기준으로 "해금됐지만 도감에서 아직 안 읽은" finishKey들을 채움
    /// </summary>
    private void RefreshUnseenFinishKeys()
    {
        _unseenFinishKeys.Clear();

        if (allEntries == null || allEntries.Count == 0 || UnlockManager.Instance == null)
        {
            if (dogamAlertIcon != null) dogamAlertIcon.SetActive(false);
            return;
        }

        foreach (var e in allEntries)
        {
            if (!IsEntryUnlocked(e)) continue;          // 아직 잠금이면 패스

            var finishKey = GetFinishKey(e);           // 이미 DoGam 내부에 있는 함수 재사용
            if (string.IsNullOrEmpty(finishKey)) continue;

            if (!_seenFinishKeys.Contains(finishKey))
                _unseenFinishKeys.Add(finishKey);
        }

        // 도감 열기 버튼 느낌표 on/off
        if (dogamAlertIcon != null)
            dogamAlertIcon.SetActive(_unseenFinishKeys.Count > 0);
    }

    /// <summary>
    /// 현재 DoGamEntry를 "읽음 처리"
    /// </summary>
    private void MarkEntrySeen(DoGamEntry entry)
    {
        if (entry == null) return;

        var finishKey = GetFinishKey(entry);
        if (string.IsNullOrEmpty(finishKey)) return;

        if (_seenFinishKeys.Contains(finishKey)) return;  // 이미 처리된 경우

        _seenFinishKeys.Add(finishKey);
        _unseenFinishKeys.Remove(finishKey);
        SaveSeenFinishKeys();

        // 더 이상 새 레시피가 없다면 도감 버튼 느낌표도 끈다
        if (dogamAlertIcon != null && _unseenFinishKeys.Count == 0)
            dogamAlertIcon.SetActive(false);
    }

    // 도감 상태용 PlayerPrefs 키를 세이브(서버)별로 분리하는 유틸
    private string GetDoGamStateKey(string baseKey)
    {
        string server = PlayerPrefs.GetString("SelectedSave", "");
        if (string.IsNullOrEmpty(server))
            return baseKey;  // 혹시 SelectedSave가 아직 없으면 기존 키 사용(안전장치)

        return server + ":" + baseKey;
    }


    // ===================== 레시피 탭 =====================
    void LoadDoGamDataFromJSON()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/DoGamData");
        if (json == null)
        {
            Debug.LogWarning("[DoGam] Data/DoGamData.json 없음");
            entryList = new List<DoGamEntry>();
            allEntries = new List<DoGamEntry>();
            doGamDict = new Dictionary<string, DoGamEntry>();
            return;
        }

        var data = JsonConvert.DeserializeObject<DoGamEntryList>(json.text);
        doGamDict = new Dictionary<string, DoGamEntry>();
        entryList = new List<DoGamEntry>(data.entries);
        allEntries = new List<DoGamEntry>(data.entries);
        foreach (var entry in data.entries) doGamDict[entry.name] = entry;

        // 초기에는 레시피 네비 안 보이게 유지
        SetRecipeNavVisible(false);
    }

    public void FilterByCategory(string category)
    {
        // 현재 카테고리 기억
        currentRecipeCategory = category;

        // 레시피 탭 활성, 게임방법 비활성
        SetRecipeLayout(true);
        SetHowToLayout(false);
        SubTitle.SetActive(false);
        SetMakerLayout(false);
        isHowToOpen = false;

        //if (lockCoverPanel) lockCoverPanel.SetActive(false);

        //_suppressLockOnce = true;
        //if (lockCoverPanel) lockCoverPanel.SetActive(false);

        // 카테고리 분류
        _allInCat = allEntries.Where(e => e.category == category).ToList();
        _unlockedInCat = _allInCat.Where(IsEntryUnlocked).ToList();
        _unlockedCount = _unlockedInCat.Count;

        _hasLockedPeek = (_unlockedCount < _allInCat.Count);

        _maxIndex = _hasLockedPeek ? _unlockedCount : Mathf.Max(0, _unlockedCount - 1);

        // entryList는 UI 바인딩용(해금만 또는 전체)
        entryList = hideLockedRecipes ? new List<DoGamEntry>(_unlockedInCat) : new List<DoGamEntry>(_allInCat);

        // 시작 페이지는 0
        _currentIndex = 0;
        UpdatePage(); // 잠금/해금/오버레이 상태 반영
        SetRecipeNavVisible(true);

        ApplyTabSpritesForCategory(category);
    }

    private void SetActiveTab(Button b)
    {
        // 모든 탭을 일반 스프라이트로 되돌림
        if (tteokButton) tteokButton.image.sprite = tabNormalSprite;
        if (drinkButton) drinkButton.image.sprite = tabNormalSprite;
        if (howToButton) howToButton.image.sprite = tabNormalSprite;
        if (makerButton) makerButton.image.sprite = tabNormalSprite;

        // 현재 탭만 pressed 스프라이트로 고정
        _activeTabButton = b;
        if (_activeTabButton) _activeTabButton.image.sprite = tabPressedSprite;

        // 탭 버튼은 전환 애니메이션 영향 안 받도록
        if (tteokButton) tteokButton.transition = Selectable.Transition.None;
        if (drinkButton) drinkButton.transition = Selectable.Transition.None;
        if (howToButton) howToButton.transition = Selectable.Transition.None;
        if (makerButton) makerButton.transition = Selectable.Transition.None;
    }

    private void ApplyTabSpritesForCategory(string category)
    {
        if (category == "떡") SetActiveTab(tteokButton);
        else if (category == "음료") SetActiveTab(drinkButton);
    }

    private void UpdateRewardInfo(DoGamEntry entry)
    {
        if (rewardInfoRoot == null) return;

        // entry가 없거나 값이 0이면 숨김
        if (entry == null || (entry.rewardStarlight <= 0 && entry.rewardExp <= 0))
        {
            rewardInfoRoot.SetActive(false);
            return;
        }

        // 떡 / 음료만 보이게 하고 싶으면 카테고리 체크 추가
        if (entry.category != "떡" && entry.category != "음료")
        {
            rewardInfoRoot.SetActive(false);
            return;
        }

        rewardInfoRoot.SetActive(true);

        if (rewardCurrencyText != null)
            rewardCurrencyText.text = entry.rewardStarlight.ToString();

        if (rewardExpText != null)
            rewardExpText.text = entry.rewardExp.ToString();

    }


    /// <summary>
    /// 페이지(해금/잠금 첫 페이지) 상태를 갱신한다.
    /// </summary>
    private void UpdatePage()
    {
        // 범위: 0.._unlockedCount (마지막+1 = 잠금 첫 페이지)
        if (_currentIndex < 0) _currentIndex = 0;
        if (_currentIndex > _unlockedCount) _currentIndex = _unlockedCount;

        bool onLockedPeek = _hasLockedPeek && (_currentIndex == _unlockedCount);

        if (_suppressLockOnce)
        {
            if (lockCoverPanel) lockCoverPanel.SetActive(false);
            _suppressLockOnce = false; // 한 번만 적용
        }
        else
        {
            if (lockCoverPanel) lockCoverPanel.SetActive(onLockedPeek);
        }

        // ② 콘텐츠 표시
        if (!onLockedPeek)
        {
            // 해금 리스트가 비었으면 아무 것도 표시하지 않음
            if (_unlockedCount > 0)
            {
                ShowEntryUnlocked(_currentIndex);
            }
            else
            {
                // 해금이 하나도 없을 때(선택) - 오버레이가 가리므로 비워둬도 무방
                ClearRecipeLines();
                if (itemImage) itemImage.sprite = null;
                if (nameText) nameText.text = "";
                if (descriptionText) descriptionText.text = "";
                if (rewardInfoRoot != null) rewardInfoRoot.SetActive(false);
            }
        }
        else
        {
            // 잠금 페이지에서는 콘텐츠 표시 생략(오버레이가 가림)
            ClearRecipeLines();
            if (itemImage) itemImage.sprite = null;
            if (nameText) nameText.text = "";
            if (descriptionText) descriptionText.text = "";
            if (recipeText) recipeText.text = "";
            if (rewardInfoRoot != null) rewardInfoRoot.SetActive(false);
        }

        // 내비게이션 버튼 상태
        //if (prevButton) prevButton.interactable = (_currentIndex > 0);
        //if (nextButton) nextButton.interactable = true; // 잠금 페이지에서도 눌러도 더는 넘어가지 않음

        // 내비게이션 버튼 상태
        if (prevButton)
        {
            bool showPrev = (_currentIndex > 0);
            prevButton.gameObject.SetActive(showPrev);
            //prevButton.interactable = showPrev;
        }

        // 마지막 페이지(= _unlockedCount, 잠금 첫 페이지 포함)면 next 비활성
        if (nextButton)
        {
            bool showNext = (_currentIndex < _maxIndex);
            nextButton.gameObject.SetActive(showNext);
            //nextButton.interactable = showNext;
        }

        // 잠금 페이지나 해금 0개일 때는 NEW 아이콘 숨김
        if (recipeAlertIcon != null)
        {
            if (onLockedPeek || _unlockedCount == 0)
                recipeAlertIcon.SetActive(false);
        }
    }

    /// <summary>
    /// 해금 리스트 기준으로 표시(실제 ShowEntry는 entryList 인덱스를 요구하므로 매핑)
    /// </summary>
    private void ShowEntryUnlocked(int unlockedIndex)
    {
        var entry = _unlockedInCat[unlockedIndex];

        // 1) 이 페이지가 새 레시피인지 먼저 체크
        var finishKey = GetFinishKey(entry);
        bool isNew = !string.IsNullOrEmpty(finishKey) && _unseenFinishKeys.Contains(finishKey);

        // entryList가 해금 전용이면 인덱스 동일, 전체 리스트면 매핑 필요
        int idxInCurrentList = entryList.IndexOf(entry);
        if (idxInCurrentList < 0) idxInCurrentList = 0;

        currentIndex = idxInCurrentList; // legacy 인덱스 유지
        ShowEntry(currentIndex);

        MarkEntrySeen(entry);

        // 페이지용 NEW 아이콘 갱신
        if (recipeAlertIcon != null)
            recipeAlertIcon.SetActive(isNew);
    }

    public void ShowEntry(int index)
    {
        // 1) 기본 범위/널 체크
        if (entryList == null || entryList.Count == 0) return;
        if (index < 0 || index >= entryList.Count) return;

        var entry = entryList[index];

        // 2) 상단 정보 바인딩
        if (itemImage) itemImage.sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + entry.image);
        if (nameText) nameText.text = entry.name;
        //if (descriptionText) nameText.text = entry.name; // (오타 방지: 필요시 descriptionText 로 아래 줄 사용)
        if (descriptionText) descriptionText.text = entry.description;

        UpdateRewardInfo(entry);

        //// 3) 기존 라인 정리
        //if (recipeContentParent != null)
        //{
        //    for (int c = recipeContentParent.childCount - 1; c >= 0; c--)
        //        Destroy(recipeContentParent.GetChild(c).gameObject);
        //}

        //// 안전한 수 계산
        //int recipeCount = entry.recipe != null ? entry.recipe.Count : 0;
        //int bundleCount = (entry.recipeImageBundle != null) ? entry.recipeImageBundle.Count : 0;
        //int linesWithImages = Mathf.Min(recipeCount, bundleCount);

        //// 4) 이미지 포함 라인 렌더 (0 .. linesWithImages-1)
        //for (int i = 0; i < linesWithImages; i++)
        //{
        //    var bundle = entry.recipeImageBundle[i];
        //    int ingredientCount = Mathf.Clamp(bundle?.ingredients != null ? bundle.ingredients.Count : 0, 1, 4);
        //    string bgPrefabName = $"RecipeLineBG_{ingredientCount}";
        //    var prefab = Resources.Load<GameObject>($"RecipeLine/{bgPrefabName}");

        //    if (prefab == null)
        //    {
        //        Debug.LogWarning($"[DoGam] 배경 프리팹 {bgPrefabName} 을 찾을 수 없습니다. i={i}");
        //        continue;
        //    }

        //    var lineGO = Instantiate(prefab, recipeContentParent);

        //    // 텍스트
        //    var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
        //    if (text != null && i < recipeCount) text.text = entry.recipe[i];

        //    // 슬롯들
        //    var toolSlot = lineGO.transform.Find("ToolSlot");
        //    var resultSlot = lineGO.transform.Find("ResultSlot");
        //    var ingSlots = new List<Transform>
        //    {
        //        lineGO.transform.Find("IngredientSlot1"),
        //        lineGO.transform.Find("IngredientSlot2"),
        //        lineGO.transform.Find("IngredientSlot3"),
        //        lineGO.transform.Find("IngredientSlot4")
        //    };

        //    // 제작기
        //    if (!string.IsNullOrEmpty(bundle.tool) && toolSlot != null)
        //    {
        //        var go = Instantiate(recipeImagePrefab, toolSlot);
        //        go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
        //        var img = go.GetComponent<Image>();
        //        string toolName = System.IO.Path.GetFileNameWithoutExtension(bundle.tool);
        //        var sprite = Resources.Load<Sprite>($"Sprites/restaurant/{toolName}");
        //        if (img != null)
        //        {
        //            img.sprite = sprite;
        //            img.enabled = sprite != null;
        //            if (sprite != null) img.preserveAspect = true;
        //        }
        //    }

        //    // 재료
        //    if (bundle.ingredients != null)
        //    {
        //        for (int j = 0; j < bundle.ingredients.Count && j < ingSlots.Count; j++)
        //        {
        //            if (ingSlots[j] != null)
        //            {
        //                var go = Instantiate(recipeImagePrefab, ingSlots[j]);
        //                go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
        //                var img = go.GetComponent<Image>();
        //                string ingName = System.IO.Path.GetFileNameWithoutExtension(bundle.ingredients[j]);
        //                var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{ingName}");
        //                if (img != null)
        //                {
        //                    img.sprite = sprite;
        //                    img.enabled = sprite != null;
        //                    if (sprite != null) img.preserveAspect = true;
        //                }
        //            }
        //        }
        //    }

        //    // 결과물
        //    if (!string.IsNullOrEmpty(bundle.result) && resultSlot != null)
        //    {
        //        var go = Instantiate(recipeImagePrefab, resultSlot);
        //        go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
        //        var img = go.GetComponent<Image>();
        //        string resultName = System.IO.Path.GetFileNameWithoutExtension(bundle.result);
        //        var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{resultName}");
        //        if (img != null)
        //        {
        //            img.sprite = sprite;
        //            img.enabled = sprite != null;
        //            if (sprite != null) img.preserveAspect = true;
        //        }
        //    }
        //}

        //// 5) 텍스트만 있는 라인 렌더 (linesWithImages .. recipeCount-1)
        //for (int i = linesWithImages; i < recipeCount; i++)
        //{
        //    // 이미지 번들이 없으므로 1칸 BG로 텍스트만 표기
        //    var prefab = Resources.Load<GameObject>("RecipeLine/RecipeLineBG_1");
        //    if (prefab == null)
        //    {
        //        Debug.LogWarning($"[DoGam] 기본 배경 프리팹 RecipeLineBG_1 을 찾을 수 없습니다. i={i}");
        //        continue;
        //    }

        //    var lineGO = Instantiate(prefab, recipeContentParent);

        //    var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
        //    if (text != null) text.text = entry.recipe[i];

        //    // 슬롯을 찾아도 아이콘은 생성하지 않음(텍스트-only)
        //}

        //// 6) 스크롤 맨 위로
        //if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;

        BuildRecipeLinesForEntry(entry, recipeContentParent, scrollRect);
    }

    public void NextEntry()
    {
        if (_currentIndex < _maxIndex)
        {
            _currentIndex++;
            UpdatePage();
        }
    }

    public void PrevEntry()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            UpdatePage();
        }
        // 0페이지면 더 못 감
    }

    private void ClearRecipeLines()
    {
        if (recipeContentParent == null) return;
        for (int i = recipeContentParent.childCount - 1; i >= 0; i--)
            Destroy(recipeContentParent.GetChild(i).gameObject);
    }

    // ===================== 게임방법 탭 =====================
    private void LoadHowToFromJSON()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/HowTo");
        if (json == null)
        {
            Debug.LogWarning("[HowTo] Data/HowTo.json 없음");
            howToPages = new List<HowToPageData>();
            return;
        }

        try
        {
            var data = JsonConvert.DeserializeObject<HowToBookData>(json.text);
            howToPages = (data != null && data.pages != null) ? data.pages : new List<HowToPageData>();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[HowTo] JSON 파싱 실패: " + e.Message);
            howToPages = new List<HowToPageData>();
        }
    }

    public void OpenHowToTab()
    {
        panel.SetActive(true);
        SubTitle.SetActive(true);
        openButton.interactable = false;

        // 레이아웃 전환: 레시피 Off, 게임방법 On
        SetRecipeLayout(false);
        SetHowToLayout(true);
        SetMakerLayout(false);
        isHowToOpen = true;

        SetActiveTab(howToButton);

        if (howToPages == null || howToPages.Count == 0)
            LoadHowToFromJSON();

        howToSpreadIndex = 0;
        RenderHowToSpread();

        if (lockCoverPanel) lockCoverPanel.SetActive(false);
    }

    private void ChangeHowToSpread(int delta)
    {
        if (howToPages == null || howToPages.Count == 0) return;
        howToSpreadIndex += delta;
        howToSpreadIndex = Mathf.Clamp(howToSpreadIndex, 0, howToPages.Count - 1);
        RenderHowToSpread();
    }

    private void RenderHowToSpread()
    {
        if (howToRoot == null) return;

        // 부모 비우기
        ClearChildren(howToLeftPageParent);
        ClearChildren(howToRightPageParent);

        if (howToPages == null || howToPages.Count == 0) return;
        var page = howToPages[Mathf.Clamp(howToSpreadIndex, 0, howToPages.Count - 1)];

        // 스프레드 제목
        if (howToTitleText != null)
            howToTitleText.text = page.title;

        // 항목 4개 (왼쪽 2, 오른쪽 2) 배치
        int count = page.items != null ? Mathf.Min(page.items.Count, howToItemsPerSpread) : 0;
        for (int i = 0; i < count; i++)
        {
            var parent = (i < 2) ? howToLeftPageParent : howToRightPageParent;
            var item = page.items[i];

            var go = Instantiate(howToItemPrefab, parent);

            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            var body = go.transform.Find("Body")?.GetComponent<TextMeshProUGUI>();

            if (body != null) body.text = item.text;

            if (icon != null)
            {
                var sprite = Resources.Load<Sprite>("Sprites/Guide/" + item.image);
                if (sprite == null)
                {
                    var all = Resources.LoadAll<Sprite>("Sprites/Guide/" + item.image);
                    if (all != null && all.Length > 0) sprite = all[0];
                }

                icon.sprite = sprite;
                icon.enabled = sprite != null;

                if (sprite != null)
                {
                    icon.preserveAspect = true;
                    LayoutRebuilder.MarkLayoutForRebuild(icon.rectTransform);
                }
            }
        }

        // 버튼 상태
        bool showPrev = (howToSpreadIndex > 0);
        bool showNext = (howToSpreadIndex < howToPages.Count - 1);

        if (howToPrevButton != null)
        {
            howToPrevButton.gameObject.SetActive(showPrev);
            howToPrevButton.interactable = showPrev;
        }
        if (howToNextButton != null)
        {
            howToNextButton.gameObject.SetActive(showNext);
            howToNextButton.interactable = showNext;
        }
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    //==========제작기 탭==============
    private void SetMakerLayout(bool on)
    {
        if (makerRoot != null) makerRoot.SetActive(on);
        SetMakerNavVisible(on && makerItems.Count > 0);
        if (on && lockCoverPanel) lockCoverPanel.SetActive(false);
    }

    private void SetMakerNavVisible(bool on)
    {
        if (!on)
        {
            if (makerPrevButton != null) makerPrevButton.gameObject.SetActive(false);
            if (makerNextButton != null) makerNextButton.gameObject.SetActive(false);
        }
        else
        {
            // 켤 때는 현재 스프레드 기준으로 버튼 보이기/숨기기 재계산
            RenderMakerSpread();
        }
    }

    private void LoadMakerFromJSON()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/Maker");
        if (json == null)
        {
            Debug.LogWarning("[Maker] Data/Maker.json 없음");
            makerItems = new List<MakerItemData>();
            return;
        }
        try
        {
            var data = JsonConvert.DeserializeObject<MakerBookData>(json.text);
            makerItems = (data != null && data.items != null) ? data.items : new List<MakerItemData>();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Maker] JSON 파싱 실패: " + e.Message);
            makerItems = new List<MakerItemData>();
        }
    }

    public void OpenMakerTab()
    {
        panel.SetActive(true);
        SubTitle.SetActive(false);
        openButton.interactable = false;

        SetRecipeLayout(false);
        SetHowToLayout(false);
        

        if (makerItems == null || makerItems.Count == 0)
            LoadMakerFromJSON();

        SetMakerLayout(true);
        SetActiveTab(makerButton);

        makerSpreadIndex = 0;
        RenderMakerSpread();
        if (lockCoverPanel) lockCoverPanel.SetActive(false);
    }

    private void ChangeMakerSpread(int delta)
    {
        if (makerItems == null || makerItems.Count == 0) return;
        int spreadCount = Mathf.CeilToInt((float)makerItems.Count / makerItemsPerSpread);
        makerSpreadIndex = Mathf.Clamp(makerSpreadIndex + delta, 0, Mathf.Max(0, spreadCount - 1));
        RenderMakerSpread();
    }


    //public int makerItemsPerSpread = 4; // 좌2 + 우2

    private void RenderMakerSpread()
    {
        if (makerRoot == null) return;

        // 부모 비우기
        ClearChildren(makerLeftPageParent);
        ClearChildren(makerRightPageParent);

        if (makerItems == null || makerItems.Count == 0) return;

        // 스프레드 범위 계산
        int start = makerSpreadIndex * makerItemsPerSpread;
        int end = Mathf.Min(start + makerItemsPerSpread, makerItems.Count);

        for (int i = start; i < end; i++)
        {
            var parent = ((i - start) < 2) ? makerLeftPageParent : makerRightPageParent;
            var item = makerItems[i];

            var go = Instantiate(makerItemPrefab != null ? makerItemPrefab : howToItemPrefab, parent);
            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            var name = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            var body = go.transform.Find("Body")?.GetComponent<TextMeshProUGUI>();

            if (name != null) name.text = item.name;
            if (body != null) body.text = item.desc;

            if (icon != null)
            {
                var sprite = Resources.Load<Sprite>("Sprites/restaurant/" + item.image);
                if (sprite == null)
                {
                    var all = Resources.LoadAll<Sprite>("Sprites/restaurant/" + item.image);
                    if (all != null && all.Length > 0) sprite = all[0];
                }
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                if (sprite != null)
                {
                    icon.preserveAspect = true;
                    LayoutRebuilder.MarkLayoutForRebuild(icon.rectTransform);
                }
            }
        }

        // 내비 버튼 상태
        int spreadCount = Mathf.CeilToInt((float)makerItems.Count / makerItemsPerSpread);
        bool showPrev = (makerSpreadIndex > 0);
        bool showNext = (makerSpreadIndex < spreadCount - 1);

        if (makerPrevButton != null)
        {
            makerPrevButton.gameObject.SetActive(showPrev);
            makerPrevButton.interactable = showPrev;
        }
        if (makerNextButton != null)
        {
            makerNextButton.gameObject.SetActive(showNext);
            makerNextButton.interactable = showNext;
        }

    }



    //==========잠금 판정==============
    // 도감 엔트리의 "완성 키" 추출: recipeImageBundle의 마지막 result → 없으면 대표 이미지 파일명
    private string GetFinishKey(DoGamEntry e)
    {
        // 1) 레시피 번들 중 result가 있는 마지막 항목을 우선
        if (e.recipeImageBundle != null && e.recipeImageBundle.Count > 0)
        {
            var lastWithResult = e.recipeImageBundle
                .Where(b => b != null && !string.IsNullOrEmpty(b.result))
                .LastOrDefault();
            if (lastWithResult != null)
                return Path.GetFileNameWithoutExtension(lastWithResult.result);
        }

        // 2) 번들이 비었으면, 엔트리 대표 이미지(완성품)가 곧 키라고 가정
        if (!string.IsNullOrEmpty(e.image))
            return Path.GetFileNameWithoutExtension(e.image);

        return null; // 키를 유추 못하면 잠금 처리
    }

    // “완성 키”가 해금되어야만 도감에서 보이도록
    private bool IsEntryUnlocked(DoGamEntry e)
    {
        var um = UnlockManager.Instance;
        if (um == null) return true; // 초기 로딩 안전장치

        var finishKey = GetFinishKey(e);
        if (string.IsNullOrWhiteSpace(finishKey)) return false; // 보수적으로 잠금

        return um.IsRecipeUnlocked(finishKey);
    }

    // 미니버전에서도 재사용 가능한 레시피 라인 빌더
    public void BuildRecipeLinesForEntry(DoGamEntry entry, Transform targetContentParent, ScrollRect targetScrollRect = null)
    {
        if (targetContentParent == null) return;

        // entry가 null이면 그냥 비우기만
        for (int c = targetContentParent.childCount - 1; c >= 0; c--)
            Destroy(targetContentParent.GetChild(c).gameObject);
        if (entry == null) return;

        int recipeCount = entry.recipe != null ? entry.recipe.Count : 0;
        int bundleCount = (entry.recipeImageBundle != null) ? entry.recipeImageBundle.Count : 0;
        int linesWithImages = Mathf.Min(recipeCount, bundleCount);

        for (int i = 0; i < linesWithImages; i++)
        {
            var bundle = entry.recipeImageBundle[i];
            int ingredientCount = Mathf.Clamp(bundle?.ingredients != null ? bundle.ingredients.Count : 0, 1, 4);
            string bgPrefabName = $"RecipeLineBG_{ingredientCount}";
            var prefab = Resources.Load<GameObject>($"RecipeLine/{bgPrefabName}");

            if (prefab == null)
            {
                Debug.LogWarning($"[DoGam] 배경 프리팹 {bgPrefabName} 을 찾을 수 없습니다. i={i}");
                continue;
            }

            var lineGO = Instantiate(prefab, targetContentParent);

            // 텍스트
            var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null && i < recipeCount) text.text = entry.recipe[i];

            // 슬롯들
            var toolSlot = lineGO.transform.Find("ToolSlot");
            var resultSlot = lineGO.transform.Find("ResultSlot");
            var ingSlots = new List<Transform>
        {
            lineGO.transform.Find("IngredientSlot1"),
            lineGO.transform.Find("IngredientSlot2"),
            lineGO.transform.Find("IngredientSlot3"),
            lineGO.transform.Find("IngredientSlot4")
        };

            // 제작기
            if (!string.IsNullOrEmpty(bundle.tool) && toolSlot != null)
            {
                var go = Instantiate(recipeImagePrefab, toolSlot);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
                var img = go.GetComponent<Image>();
                string toolName = System.IO.Path.GetFileNameWithoutExtension(bundle.tool);
                var sprite = Resources.Load<Sprite>($"Sprites/restaurant/{toolName}");
                if (img != null)
                {
                    img.sprite = sprite;
                    img.enabled = sprite != null;
                    if (sprite != null) img.preserveAspect = true;
                }
            }

            // 재료
            if (bundle.ingredients != null)
            {
                for (int j = 0; j < bundle.ingredients.Count && j < ingSlots.Count; j++)
                {
                    if (ingSlots[j] != null)
                    {
                        var go = Instantiate(recipeImagePrefab, ingSlots[j]);
                        go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
                        var img = go.GetComponent<Image>();
                        string ingName = System.IO.Path.GetFileNameWithoutExtension(bundle.ingredients[j]);
                        var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{ingName}");
                        if (img != null)
                        {
                            img.sprite = sprite;
                            img.enabled = sprite != null;
                            if (sprite != null) img.preserveAspect = true;
                        }
                    }
                }
            }

            // 결과물
            if (!string.IsNullOrEmpty(bundle.result) && resultSlot != null)
            {
                var go = Instantiate(recipeImagePrefab, resultSlot);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
                var img = go.GetComponent<Image>();
                string resultName = System.IO.Path.GetFileNameWithoutExtension(bundle.result);
                var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{resultName}");
                if (img != null)
                {
                    img.sprite = sprite;
                    img.enabled = sprite != null;
                    if (sprite != null) img.preserveAspect = true;
                }
            }
        }

        // 2) 텍스트만 있는 라인 렌더 (linesWithImages .. recipeCount-1)
        for (int i = linesWithImages; i < recipeCount; i++)
        {
            var prefab = Resources.Load<GameObject>("RecipeLine/RecipeLineBG_1");
            if (prefab == null)
            {
                Debug.LogWarning($"[DoGam] 기본 배경 프리팹 RecipeLineBG_1 을 찾을 수 없습니다. i={i}");
                continue;
            }

            var lineGO = Instantiate(prefab, targetContentParent);

            var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = entry.recipe[i];
        }

        // 3) 스크롤 맨 위로
        if (targetScrollRect != null)
            targetScrollRect.verticalNormalizedPosition = 1f;
    }

    public IReadOnlyList<DoGamEntry> GetUnlockedEntriesByCategory(string category)
    {
        if (allEntries == null) return System.Array.Empty<DoGamEntry>();

        var list = new List<DoGamEntry>();
        foreach (var e in allEntries)
        {
            if (e.category != category) continue; // 카테고리 필터
            if (!IsEntryUnlocked(e)) continue;    // 잠긴 레시피 제외
            list.Add(e);
        }
        return list;
    }

    public int GetTotalEntriesByCategory(string category)
    {
        if (allEntries == null) return 0;

        int count = 0;
        foreach (var e in allEntries)
        {
            if (e.category == category)
                count++;
        }
        return count;
    }
}


