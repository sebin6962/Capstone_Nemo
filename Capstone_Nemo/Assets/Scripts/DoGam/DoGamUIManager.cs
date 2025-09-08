using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using System.IO;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class DoGamUIManager : MonoBehaviour
{
    public static DoGamUIManager Instance;

    public GameObject panel;
    public Button openButton;
    public Button closeButton;

    public Button nextButton;
    public Button prevButton;

    public Button tteokButton;
    public Button drinkButton;
    public Button guestButton;

    public ScrollRect scrollRect;

    private int currentIndex = 0;

    private List<DoGamEntry> entryList = new(); // 현재 표시할 목록
    private List<DoGamEntry> allEntries = new(); // 전체 목록

    public Image itemImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI recipeText;

    public Transform recipeContentParent;
    //public Transform recipeImageParent; // 레시피 이미지들이 붙을 부모 오브젝트
    public GameObject recipeImagePrefab; // Image만 있는 프리팹
    public GameObject recipeLineBackgroundPrefab;
    private Dictionary<string, DoGamEntry> doGamDict;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        openButton.onClick.AddListener(() => OpenDoGam("백설기"));
        closeButton.onClick.AddListener(CloseDoGam);

        tteokButton.onClick.AddListener(() => FilterByCategory("떡"));
        drinkButton.onClick.AddListener(() => FilterByCategory("음료"));
        guestButton.onClick.AddListener(() => FilterByCategory("손님"));

        panel.SetActive(false);
        prevButton.gameObject.SetActive(false); // 버튼 숨기기
        nextButton.gameObject.SetActive(false); // 버튼 숨기기
        LoadDoGamDataFromJSON();
    }

    private void Start()
    {
        nextButton.onClick.AddListener(() => NextEntry());
        prevButton.onClick.AddListener(() => PrevEntry());
    }

    private void Update()
    {
        if (!panel.activeSelf) return;

        //페이지 스크롤, 페이지 클릭으로 넘기기
        //float scroll = Input.GetAxis("Mouse ScrollWheel");

        //if (scroll > 0f)
        //{
        //    PrevEntry();
        //}
        //else if (scroll < 0f)
        //{
        //    NextEntry();
        //}

        //if (Input.GetMouseButtonDown(0))
        //{
        //    PointerEventData data = new PointerEventData(EventSystem.current);
        //    data.position = Input.mousePosition;

        //    List<RaycastResult> results = new List<RaycastResult>();
        //    EventSystem.current.RaycastAll(data, results);

        //    foreach (var r in results)
        //    {
        //        Debug.Log("Raycast hit: " + r.gameObject.name);
        //    }
        //}
    }

    void LoadDoGamDataFromJSON()
    {
        TextAsset json = Resources.Load<TextAsset>("Data/DoGamData");

        var data = JsonConvert.DeserializeObject<DoGamEntryList>(json.text);

        doGamDict = new Dictionary<string, DoGamEntry>();
        entryList = new List<DoGamEntry>(data.entries);
        allEntries = new List<DoGamEntry>(data.entries);

        foreach (var entry in data.entries)
            doGamDict[entry.name] = entry;

        Debug.Log($"[도감 로딩] 항목 수: {entryList.Count}");
    }

    public void OpenDoGam(string itemName)
    {
        // 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (BoxInventoryManager.Instance != null && BoxInventoryManager.Instance.IsInventoryOpen())
            return;

        // 가게 박스 인벤토리 열려 있으면 도감 오픈 막기
        if (PlayerStoreBoxInventoryUIManager.Instance != null && PlayerStoreBoxInventoryUIManager.Instance.IsOpen())
            return;

        if (!doGamDict.ContainsKey(itemName))
        {
            Debug.LogWarning($"도감 항목 '{itemName}'을 찾을 수 없습니다.");
            return;
        }

        FilterByCategory("떡"); // 도감 열면 무조건 '떡' 필터 적용

        // 버튼을 가장 위로 올림 (레이캐스트 우선순위 확보)
        prevButton.transform.SetAsLastSibling();
        nextButton.transform.SetAsLastSibling();

        SFXManager.Instance.PlayBbyongSFX();

        var entry = doGamDict[itemName];
        panel.SetActive(true);
        prevButton.gameObject.SetActive(true); 
        nextButton.gameObject.SetActive(true);

        openButton.interactable = false;

        nameText.text = entry.name;
        descriptionText.text = entry.description;
        recipeText.text = string.Join("\n", entry.recipe);

        itemImage.sprite = Resources.Load<Sprite>("Sprites/Dagwa/" + entry.image);
    }

    public bool IsOpen()
    {
        return panel != null && panel.activeSelf;
    }

    public void ShowEntry(int index)
    {
        if (index < 0 || index >= entryList.Count) return;
        var entry = entryList[index];

        itemImage.sprite = Resources.Load<Sprite>("Sprites/Dagwa/" + entry.image);

        nameText.text = entry.name;
        descriptionText.text = entry.description;

        foreach (Transform child in recipeContentParent)
            Destroy(child.gameObject);

        // 1. 텍스트 출력 (기존대로)
        //recipeText.text = string.Join("\n", entry.recipe);

        // 2. 이미지 출력
        // 기존 이미지 오브젝트 모두 제거
        //foreach (Transform child in recipeImageParent)
        //Destroy(child.gameObject);

        for (int i = 0; i < entry.recipe.Count; i++)
        {
            //var lineGO = Instantiate(recipeLineBackgroundPrefab, recipeContentParent);
            var bundle = entry.recipeImageBundle[i];
            int ingredientCount = bundle.ingredients.Count;
            ingredientCount = Mathf.Clamp(ingredientCount, 1, 4); // 예외 방지

            string bgPrefabName = $"RecipeLineBG_{ingredientCount}";
            var prefab = Resources.Load<GameObject>($"RecipeLine/{bgPrefabName}");

            if (prefab == null)
            {
                Debug.LogWarning($"[도감] 배경 프리팹 {bgPrefabName} 을 찾을 수 없습니다.");
                continue;
            }

            var lineGO = Instantiate(prefab, recipeContentParent);
            var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = entry.recipe[i];

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
                //var bundle = entry.recipeImageBundle[i];

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
                        img.enabled = sprite != null;
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
                    img.enabled = sprite != null;
                }
            }
        }


        
        scrollRect.verticalNormalizedPosition = 1f;
    }

    public void FilterByCategory(string category)
    {
        entryList = allEntries.FindAll(e => e.category == category);
        if (entryList.Count == 0)
        {
            Debug.LogWarning($"카테고리 '{category}'에 해당하는 레시피가 없습니다.");
            return;
        }

        currentIndex = 0;
        ShowEntry(currentIndex);
    }

    public void CloseDoGam()
    {
        SFXManager.Instance.PlayBbyongSFX();
        panel.SetActive(false);
        prevButton.gameObject.SetActive(false); // 버튼 숨기기
        nextButton.gameObject.SetActive(false); // 버튼 숨기기

        openButton.interactable = true;
    }

    public void NextEntry()
    {
        if (entryList.Count == 0) return;

        if (currentIndex < entryList.Count - 1)
        {
            currentIndex++;
            ShowEntry(currentIndex);
            Debug.Log("다음 버튼 클릭");
        }
    }

    public void PrevEntry()
    {
        if (entryList.Count == 0) return;

        if (currentIndex > 0)
        {
            currentIndex--;
            ShowEntry(currentIndex);
            Debug.Log("이전 버튼 클릭");
        }
    }
}
