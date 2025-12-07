using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.IO;

public class RecipeQuickViewUI : MonoBehaviour
{
    [Header("UI")]
    public ScrollRect miniScrollRect;
    public Transform miniContentParent;
    public Button prevButton;
    public Button nextButton;

    public Button tteokTabButton;
    public Button drinkTabButton;

    public Image topItemImage;
    public TextMeshProUGUI topItemName;

    public GameObject lockIconObject;

    [Header("Mini Prefab Override")]
    public GameObject miniRecipeImagePrefab;

    [Header("설정")]
    public string defaultCategory = "떡";   // 시작 카테고리

    private string _currentCategory;
    private List<DoGamEntry> _entries = new();
    private int _index = 0;

    private int _unlockedCount = 0;  
    private int _totalCount = 0;     
    private int _maxIndex = 0;        
    private bool _hasLockedPeek = false;

    private Sprite _tteokNormalSprite;
    private Sprite _drinkNormalSprite;

    void Start()
    {
        // 시작 카테고리
        _currentCategory = string.IsNullOrEmpty(defaultCategory) ? "떡" : defaultCategory;

        // 기본 스프라이트 저장
        if (tteokTabButton != null)
            _tteokNormalSprite = tteokTabButton.image != null ? tteokTabButton.image.sprite : null;

        if (drinkTabButton != null)
            _drinkNormalSprite = drinkTabButton.image != null ? drinkTabButton.image.sprite : null;

        if (prevButton != null) prevButton.onClick.AddListener(() => ChangeIndex(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => ChangeIndex(+1));

        // 미니 탭용 떡/음료 버튼
        if (tteokTabButton != null)
            tteokTabButton.onClick.AddListener(() => OnClickCategory("떡"));

        if (drinkTabButton != null)
            drinkTabButton.onClick.AddListener(() => OnClickCategory("음료"));

        ReloadList();
        UpdateCategoryTabVisual();
    }

    // 미니도감 전용
    private void BuildMiniRecipeLinesForEntry(DoGamEntry entry)
    {
        if (miniContentParent == null) return;

        // 기존 라인 비우기
        for (int c = miniContentParent.childCount - 1; c >= 0; c--)
            Destroy(miniContentParent.GetChild(c).gameObject);

        if (entry == null) return;

        var dogam = DoGamUIManager.Instance;
        if (dogam == null) return;

        // 아이콘 프리팹: 미니가 지정되어 있으면 그걸, 아니면 도감 기본 아이콘 프리팹 사용
        GameObject iconPrefab = miniRecipeImagePrefab != null ? miniRecipeImagePrefab : dogam.recipeImagePrefab;

        int recipeCount = entry.recipe != null ? entry.recipe.Count : 0;
        int bundleCount = (entry.recipeImageBundle != null) ? entry.recipeImageBundle.Count : 0;
        int linesWithImages = Mathf.Min(recipeCount, bundleCount);

        // 1) 이미지 포함 라인
        for (int i = 0; i < linesWithImages; i++)
        {
            var bundle = entry.recipeImageBundle[i];
            int ingredientCount = Mathf.Clamp(
                bundle?.ingredients != null ? bundle.ingredients.Count : 0,
                1, 4
            );

            string bgPrefabName = $"RecipeLineMiniBG_{ingredientCount}";
            var prefab = Resources.Load<GameObject>($"RecipeLineMini/{bgPrefabName}");

            if (prefab == null)
            {
                Debug.LogWarning($"[MiniDoGam] 배경 프리팹 {bgPrefabName} 을 찾을 수 없습니다. i={i}");
                continue;
            }

            var lineGO = Instantiate(prefab, miniContentParent);

            // 텍스트
            var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null && i < recipeCount)
                text.text = entry.recipe[i];

            // 슬롯 찾기 (미니 프리팹에도 동일한 이름으로 슬롯이 있어야 함!)
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
            if (!string.IsNullOrEmpty(bundle.tool) && toolSlot != null && iconPrefab != null)
            {
                var go = Instantiate(iconPrefab, toolSlot);
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(50, 50); // 필요하면 여기서도 더 줄여도 됨

                var img = go.GetComponent<Image>();
                string toolName = Path.GetFileNameWithoutExtension(bundle.tool);
                var sprite = Resources.Load<Sprite>($"Sprites/restaurant/{toolName}");
                if (img != null)
                {
                    img.sprite = sprite;
                    img.enabled = sprite != null;
                    if (sprite != null) img.preserveAspect = true;
                }
            }

            // 재료
            if (bundle.ingredients != null && iconPrefab != null)
            {
                for (int j = 0; j < bundle.ingredients.Count && j < ingSlots.Count; j++)
                {
                    if (ingSlots[j] == null) continue;

                    var go = Instantiate(iconPrefab, ingSlots[j]);
                    var rt = go.GetComponent<RectTransform>();
                    if (rt != null) rt.sizeDelta = new Vector2(50, 50);

                    var img = go.GetComponent<Image>();
                    string ingName = Path.GetFileNameWithoutExtension(bundle.ingredients[j]);
                    var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{ingName}");
                    if (img != null)
                    {
                        img.sprite = sprite;
                        img.enabled = sprite != null;
                        if (sprite != null) img.preserveAspect = true;
                    }
                }
            }

            // 결과물
            if (!string.IsNullOrEmpty(bundle.result) && resultSlot != null && iconPrefab != null)
            {
                var go = Instantiate(iconPrefab, resultSlot);
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(50, 50);

                var img = go.GetComponent<Image>();
                string resultName = Path.GetFileNameWithoutExtension(bundle.result);
                var sprite = Resources.Load<Sprite>($"Sprites/Ingredients/{resultName}");
                if (img != null)
                {
                    img.sprite = sprite;
                    img.enabled = sprite != null;
                }
            }
        }
        // 2) 텍스트만 있는 라인 (이미지 없는 recipe 줄)
        for (int i = linesWithImages; i < recipeCount; i++)
        {
            var prefab = Resources.Load<GameObject>("RecipeLineMini/RecipeLineMiniBG_1");
            if (prefab == null)
            {
                Debug.LogWarning($"[MiniDoGam] 기본 배경 프리팹 RecipeLineMiniBG_1 을 찾을 수 없습니다. i={i}");
                continue;
            }

            var lineGO = Instantiate(prefab, miniContentParent);
            var text = lineGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = entry.recipe[i];
        }

        // 3) 스크롤 맨 위로
        if (miniScrollRect != null)
            miniScrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateTopItemUI(DoGamEntry entry)
    {
        // 이미지
        if (topItemImage != null)
        {
            if (entry == null || string.IsNullOrEmpty(entry.image))
            {
                topItemImage.sprite = null;
                topItemImage.enabled = false;
            }
            else
            {
                var sprite = Resources.Load<Sprite>("Sprites/Ingredients/" + entry.image);
                topItemImage.sprite = sprite;
                topItemImage.enabled = sprite != null;
                if (sprite != null) topItemImage.preserveAspect = true;
            }
        }

        // 이름
        if (topItemName != null)
        {
            topItemName.text = entry != null ? entry.name : "";
        }
    }



    private void OnClickCategory(string cat)
    {
        if (_currentCategory == cat) return;

        _currentCategory = cat;
        _index = 0;          // 새 카테고리로 바꾸면 첫 레시피부터
        ReloadList();
        UpdateCategoryTabVisual();
    }

    private void UpdateCategoryTabVisual()
    {
        // 떡 탭
        if (tteokTabButton != null && tteokTabButton.image != null)
        {
            var state = tteokTabButton.spriteState;
            if (_currentCategory == "떡" && state.selectedSprite != null)
            {
                tteokTabButton.image.sprite = state.selectedSprite;
            }
            else
            {
                tteokTabButton.image.sprite = _tteokNormalSprite;
            }
        }

        // 음료 탭
        if (drinkTabButton != null && drinkTabButton.image != null)
        {
            var state = drinkTabButton.spriteState;
            if (_currentCategory == "음료" && state.selectedSprite != null)
            {
                drinkTabButton.image.sprite = state.selectedSprite;
            }
            else
            {
                drinkTabButton.image.sprite = _drinkNormalSprite;
            }
        }
    }



    public void ReloadList()
    {
        var dogam = DoGamUIManager.Instance;
        if (dogam == null)
        {
            _entries = new List<DoGamEntry>();
            _unlockedCount = 0;
            _totalCount = 0;
            _hasLockedPeek = false;
            _maxIndex = 0;
            RefreshView();
            return;
        }

        _entries = dogam
        .GetUnlockedEntriesByCategory(_currentCategory)
        .ToList();

        _unlockedCount = _entries.Count;

        _totalCount = dogam.GetTotalEntriesByCategory(_currentCategory);

        _hasLockedPeek = (_unlockedCount < _totalCount);

        _maxIndex = _hasLockedPeek
       ? _unlockedCount
       : Mathf.Max(0, _unlockedCount - 1);

        _index = Mathf.Clamp(_index, 0, _maxIndex);
        RefreshView();
    }

    private void ChangeIndex(int delta)
    {
        if (_totalCount == 0 && _unlockedCount == 0) return;

        int newIndex = _index + delta;
        newIndex = Mathf.Clamp(newIndex, 0, _maxIndex);

        if (newIndex == _index) return; // 더 못 가는 방향이면 무시

        _index = newIndex;
        RefreshView();
    }

    private void RefreshView()
    {
        var dogam = DoGamUIManager.Instance;
        if (dogam == null || miniContentParent == null)
            return;

        if (_totalCount == 0)
        {
            UpdateTopItemUI(null);
            BuildMiniRecipeLinesForEntry(null);

            if (prevButton != null) prevButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            return;
        }

        bool onLockedPeek = _hasLockedPeek && (_index == _unlockedCount);

        if (!onLockedPeek && _unlockedCount > 0)
        {
            int clamped = Mathf.Clamp(_index, 0, Mathf.Max(0, _unlockedCount - 1));
            var entry = _entries[clamped];

            UpdateTopItemUI(entry);
            BuildMiniRecipeLinesForEntry(entry);

            if (lockIconObject != null) lockIconObject.SetActive(false);
        }
        else
        {
            UpdateTopItemUI(null);
            BuildMiniRecipeLinesForEntry(null);

            if (lockIconObject != null) lockIconObject.SetActive(true);

        }

        if (prevButton != null)
        {
            bool showPrev = (_index > 0);
            prevButton.gameObject.SetActive(showPrev);
        }

        if (nextButton != null)
        {
            bool showNext = (_index < _maxIndex);
            nextButton.gameObject.SetActive(showNext);
        }
    }

}
