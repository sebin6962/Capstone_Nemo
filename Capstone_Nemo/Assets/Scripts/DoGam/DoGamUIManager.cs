using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using System.IO;

public class DoGamUIManager : MonoBehaviour
{
    public static DoGamUIManager Instance;

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

    private int currentIndex = 0;
    private List<DoGamEntry> entryList = new(); // 현재 표시 리스트
    private List<DoGamEntry> allEntries = new(); // 전체 리스트
    private Dictionary<string, DoGamEntry> doGamDict;

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

        // 데이터 로드
        LoadDoGamDataFromJSON();  // 레시피
        LoadHowToFromJSON();      // 게임방법
    }

    // ===================== 공통 토글 =====================
    private void SetRecipeLayout(bool on)
    {
        if (recipeRoot != null) recipeRoot.SetActive(on);
        SetRecipeNavVisible(on && entryList.Count > 0);
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
        itemImage.sprite = Resources.Load<Sprite>("Sprites/Dagwa/" + entry.image);
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

        entryList = allEntries.FindAll(e => e.category == category);
        if (entryList.Count == 0)
        {
            Debug.LogWarning($"카테고리 '{category}'에 해당하는 레시피가 없습니다.");
            // 내용 비우기
            if (itemImage) itemImage.sprite = null;
            if (nameText) nameText.text = "";
            if (descriptionText) descriptionText.text = "";
            ClearRecipeLines();
            SetRecipeNavVisible(false);
            return;
        }

        currentIndex = 0;
        ShowEntry(currentIndex);
        SetRecipeNavVisible(true);
    }

    public void ShowEntry(int index)
    {
        if (index < 0 || index >= entryList.Count) return;
        var entry = entryList[index];

        if (itemImage) itemImage.sprite = Resources.Load<Sprite>("Sprites/Dagwa/" + entry.image);
        if (nameText) nameText.text = entry.name;
        if (descriptionText) descriptionText.text = entry.description;

        // 레시피 라인 렌더링
        foreach (Transform child in recipeContentParent)
            Destroy(child.gameObject);

        for (int i = 0; i < entry.recipe.Count; i++)
        {
            var bundle = entry.recipeImageBundle[i];
            int ingredientCount = Mathf.Clamp(bundle.ingredients.Count, 1, 4);
            string bgPrefabName = $"RecipeLineBG_{ingredientCount}";
            var prefab = Resources.Load<GameObject>($"RecipeLine/{bgPrefabName}");

            if (prefab == null)
            {
                Debug.LogWarning($"[도감] 배경 프리팹 {bgPrefabName} 을 찾을 수 없습니다.");
                continue;
            }

            var lineGO = Instantiate(prefab, recipeContentParent);
            var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = entry.recipe[i];

            var toolSlot = lineGO.transform.Find("ToolSlot");
            var resultSlot = lineGO.transform.Find("ResultSlot");
            var ingSlots = new List<Transform>
            {
                lineGO.transform.Find("IngredientSlot1"),
                lineGO.transform.Find("IngredientSlot2"),
                lineGO.transform.Find("IngredientSlot3"),
                lineGO.transform.Find("IngredientSlot4")
            };

            if (entry.recipeImageBundle != null && i < entry.recipeImageBundle.Count)
            {
                // 제작기
                if (!string.IsNullOrEmpty(bundle.tool) && toolSlot != null)
                {
                    var go = Instantiate(recipeImagePrefab, toolSlot);
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
                    var img = go.GetComponent<Image>();
                    string toolName = Path.GetFileNameWithoutExtension(bundle.tool);
                    var sprite = Resources.Load<Sprite>($"Sprites/restaurant/{toolName}");
                    img.sprite = sprite;
                    img.enabled = sprite != null;


                    if (sprite != null)
                    {
                        // 핵심 1) 원본 비율 유지
                        img.preserveAspect = true;
                    }
                }

                // 재료
                for (int j = 0; j < bundle.ingredients.Count && j < ingSlots.Count; j++)
                {
                    if (ingSlots[j] != null)
                    {
                        var go = Instantiate(recipeImagePrefab, ingSlots[j]);
                        go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
                        var img = go.GetComponent<Image>();
                        string ingName = Path.GetFileNameWithoutExtension(bundle.ingredients[j]);
                        var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{ingName}");
                        img.sprite = sprite;
                        img.enabled = img != null && img.sprite != null;
                    }
                }

                // 결과물
                if (!string.IsNullOrEmpty(bundle.result) && resultSlot != null)
                {
                    var go = Instantiate(recipeImagePrefab, resultSlot);
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
                    var img = go.GetComponent<Image>();
                    string resultName = Path.GetFileNameWithoutExtension(bundle.result);
                    var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{resultName}");
                    img.sprite = sprite;
                    img.enabled = img != null && img.sprite != null;
                }
            }
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    public void NextEntry()
    {
        if (entryList.Count == 0) return;
        if (currentIndex < entryList.Count - 1)
        {
            currentIndex++;
            ShowEntry(currentIndex);
        }
    }

    public void PrevEntry()
    {
        if (entryList.Count == 0) return;
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowEntry(currentIndex);
        }
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
        // page.items.Count 가 4 미만이어도 안전하게 동작하도록 방어
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
                    // 핵심 1) 원본 비율 유지
                    icon.preserveAspect = true;

                    // 핵심 2) 부모 크기 안에서 맞추기 (부모 RectTransform 크기 기준으로 축소/확대)
                    //var arf = icon.GetComponent<AspectRatioFitter>();
                    //if (arf == null) arf = icon.gameObject.AddComponent<AspectRatioFitter>();
                    //arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

                    // 스프라이트 실제 픽셀 비율 적용
                    //arf.aspectRatio = sprite.rect.width / sprite.rect.height;

                    // (선택) 레이아웃 강제 재빌드가 필요할 때
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
}

