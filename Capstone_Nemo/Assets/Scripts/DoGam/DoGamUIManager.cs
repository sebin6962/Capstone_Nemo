using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

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
    public Button guestButton;

    [Header("잠금 오버레이")]
    [SerializeField] private GameObject lockCoverPanel;   // 도감 위를 가리는 패널(자물쇠, 블러 등)

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

    // ===================== 초기화 =====================
    private void Awake()
    {
        if (Instance == null) Instance = this;

        // 데이터 먼저 로드
        LoadDoGamDataFromJSON();  // 레시피
        LoadHowToFromJSON();      // 게임방법

        // 열기/닫기
        openButton.onClick.AddListener(() => OpenDoGam("백설기"));
        closeButton.onClick.AddListener(CloseDoGam);

        // 레시피 카테고리
        tteokButton.onClick.AddListener(() => FilterByCategory("떡"));
        drinkButton.onClick.AddListener(() => FilterByCategory("음료"));
        guestButton.onClick.AddListener(() => FilterByCategory("손님"));

        // 레시피 네비
        nextButton.onClick.AddListener(() => NextEntry());
        prevButton.onClick.AddListener(() => PrevEntry());

        // 게임방법 열기 / 네비
        if (howToButton != null) howToButton.onClick.AddListener(OpenHowToTab);
        if (howToNextButton != null) howToNextButton.onClick.AddListener(() => ChangeHowToSpread(+1));
        if (howToPrevButton != null) howToPrevButton.onClick.AddListener(() => ChangeHowToSpread(-1));

        // 초기 표시 상태
        panel.SetActive(false);
        SubTitle.SetActive(false);
        SetRecipeNavVisible(false);
        SetHowToNavVisible(false);
        if (howToRoot != null) howToRoot.SetActive(false); // 시작 시 비활성
        if (lockCoverPanel) lockCoverPanel.SetActive(false);
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
    }
    private void SetRecipeNavVisible(bool on)
    {
        if (prevButton != null) prevButton.gameObject.SetActive(on);
        if (nextButton != null) nextButton.gameObject.SetActive(on);
    }
    private void SetHowToNavVisible(bool on)
    {
        if (howToPrevButton != null) howToPrevButton.gameObject.SetActive(on);
        if (howToNextButton != null) howToNextButton.gameObject.SetActive(on);
    }

    // ===================== 도감 열기/닫기 =====================
    public void OpenDoGam(string itemName)
    {
        // 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;
        // 가게 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
            return;

        if (doGamDict == null || !doGamDict.ContainsKey(itemName))
        {
            Debug.LogWarning($"도감 항목 '{itemName}'을 찾을 수 없습니다.");
            return;
        }

        FilterByCategory("떡"); // 기본은 레시피 탭의 '떡'으로

        // 버튼을 가장 위로 (레이캐스트 우선순위)
        prevButton.transform.SetAsLastSibling();
        nextButton.transform.SetAsLastSibling();

        SFXManager.Instance.PlayBbyongSFX();

        var entry = doGamDict[itemName];
        panel.SetActive(true);

        // 레이아웃: 레시피 탭 On / 게임방법 탭 Off
        SetRecipeLayout(true);
        SetHowToLayout(false);
        SubTitle.SetActive(false);
        isHowToOpen = false;

        openButton.interactable = false;

        nameText.text = entry.name;
        descriptionText.text = entry.description;
        recipeText.text = string.Join("\n", entry.recipe);
        itemImage.sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + entry.image);

        if (lockCoverPanel) lockCoverPanel.SetActive(false);
    }

    public bool IsOpen() => panel != null && panel.activeSelf;

    public void CloseDoGam()
    {
        SFXManager.Instance.PlayBbyongSFX();
        panel.SetActive(false);
        SubTitle.SetActive(false);

        SetRecipeNavVisible(false);
        SetHowToNavVisible(false);

        if (openButton != null) openButton.interactable = true;
        isHowToOpen = false;

        if (lockCoverPanel) lockCoverPanel.SetActive(false);
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
        // 레시피 탭 활성, 게임방법 비활성
        SetRecipeLayout(true);
        SetHowToLayout(false);
        SubTitle.SetActive(false);
        isHowToOpen = false;

        if (lockCoverPanel) lockCoverPanel.SetActive(false);

        // 카테고리 분류
        _allInCat = allEntries.Where(e => e.category == category).ToList();
        _unlockedInCat = _allInCat.Where(IsEntryUnlocked).ToList();
        _unlockedCount = _unlockedInCat.Count;

        // entryList는 UI 바인딩용(해금만 또는 전체)
        entryList = hideLockedRecipes ? new List<DoGamEntry>(_unlockedInCat) : new List<DoGamEntry>(_allInCat);

        // 시작 페이지는 0
        _currentIndex = 0;
        UpdatePage(); // 잠금/해금/오버레이 상태 반영
        SetRecipeNavVisible(true);
    }

    /// <summary>
    /// 페이지(해금/잠금 첫 페이지) 상태를 갱신한다.
    /// </summary>
    private void UpdatePage()
    {
        // 범위: 0.._unlockedCount (마지막+1 = 잠금 첫 페이지)
        if (_currentIndex < 0) _currentIndex = 0;
        if (_currentIndex > _unlockedCount) _currentIndex = _unlockedCount;

        bool onLockedPeek = (_currentIndex == _unlockedCount);

        // ① 잠금 오버레이
        if (lockCoverPanel) lockCoverPanel.SetActive(onLockedPeek);

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
            }
        }
        else
        {
            // 잠금 페이지에서는 콘텐츠 표시 생략(오버레이가 가림)
        }

        // ③ 내비게이션 버튼 상태(선택)
        if (prevButton) prevButton.interactable = (_currentIndex > 0);
        if (nextButton) nextButton.interactable = true; // 잠금 페이지에서도 눌러도 더는 넘어가지 않음
    }

    /// <summary>
    /// 해금 리스트 기준으로 표시(실제 ShowEntry는 entryList 인덱스를 요구하므로 매핑)
    /// </summary>
    private void ShowEntryUnlocked(int unlockedIndex)
    {
        var entry = _unlockedInCat[unlockedIndex];

        // entryList가 해금 전용이면 인덱스 동일, 전체 리스트면 매핑 필요
        int idxInCurrentList = entryList.IndexOf(entry);
        if (idxInCurrentList < 0) idxInCurrentList = 0;

        currentIndex = idxInCurrentList; // legacy 인덱스 유지
        ShowEntry(currentIndex);
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
        if (descriptionText) nameText.text = entry.name; // (오타 방지: 필요시 descriptionText 로 아래 줄 사용)
        if (descriptionText) descriptionText.text = entry.description;

        // 3) 기존 라인 정리
        if (recipeContentParent != null)
        {
            for (int c = recipeContentParent.childCount - 1; c >= 0; c--)
                Destroy(recipeContentParent.GetChild(c).gameObject);
        }

        // 안전한 수 계산
        int recipeCount = entry.recipe != null ? entry.recipe.Count : 0;
        int bundleCount = (entry.recipeImageBundle != null) ? entry.recipeImageBundle.Count : 0;
        int linesWithImages = Mathf.Min(recipeCount, bundleCount);

        // 4) 이미지 포함 라인 렌더 (0 .. linesWithImages-1)
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

            var lineGO = Instantiate(prefab, recipeContentParent);

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

        // 5) 텍스트만 있는 라인 렌더 (linesWithImages .. recipeCount-1)
        for (int i = linesWithImages; i < recipeCount; i++)
        {
            // 이미지 번들이 없으므로 1칸 BG로 텍스트만 표기
            var prefab = Resources.Load<GameObject>("RecipeLine/RecipeLineBG_1");
            if (prefab == null)
            {
                Debug.LogWarning($"[DoGam] 기본 배경 프리팹 RecipeLineBG_1 을 찾을 수 없습니다. i={i}");
                continue;
            }

            var lineGO = Instantiate(prefab, recipeContentParent);

            var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = entry.recipe[i];

            // 슬롯을 찾아도 아이콘은 생성하지 않음(텍스트-only)
        }

        // 6) 스크롤 맨 위로
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void NextEntry()
    {
        // 마지막 해금 페이지에서 한 장 더 → 잠금 첫 페이지로 진입
        if (_currentIndex < _unlockedCount)
        {
            _currentIndex++;
            UpdatePage();
        }
        else
        {
            // 이미 잠금 첫 페이지: 더는 넘어가지 않음(효과음/진동 등 선택)
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
        isHowToOpen = true;

        if (howToPages == null || howToPages.Count == 0)
            LoadHowToFromJSON();

        howToSpreadIndex = 0;
        RenderHowToSpread();
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
        if (howToPrevButton != null) howToPrevButton.interactable = (howToSpreadIndex > 0);
        if (howToNextButton != null) howToNextButton.interactable = (howToSpreadIndex < howToPages.Count - 1);
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
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
}


