using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RecipeQuickViewUI : MonoBehaviour
{
    private const string LocalizationTable = "DoGam";

    public static RecipeQuickViewUI Instance;

    public GameObject infoText;

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
    public string defaultCategory = "떡";

    private string _currentCategory;
    private List<DoGamEntry> _entries = new();
    private int _index;

    private int _unlockedCount;
    private int _totalCount;
    private int _maxIndex;
    private bool _hasLockedPeek;

    private Sprite _tteokNormalSprite;
    private Sprite _drinkNormalSprite;
    private Sprite _miniToggleNormalSprite;
    private Sprite _miniToggleOnSprite;

    [Header("Mini Panel Toggle")]
    public GameObject miniRoot;
    public bool startVisible = true;
    public Button miniToggleButton;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        if (miniRoot != null && miniRoot.activeSelf)
            RefreshView();
    }

    private void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void DisableButtonNavigation(Button button)
    {
        if (button == null)
            return;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        button.onClick.AddListener(ClearUISelection);
    }

    private void SetLocalizedText(
        TMP_Text target,
        string key,
        string fallback)
    {
        if (target == null)
            return;

        fallback ??= string.Empty;
        target.text = fallback;

        if (!string.IsNullOrWhiteSpace(key))
            StartCoroutine(SetLocalizedTextRoutine(target, key, fallback));
    }

    private IEnumerator SetLocalizedTextRoutine(
        TMP_Text target,
        string key,
        string fallback)
    {
        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                LocalizationTable,
                key
            );

        yield return handle;

        bool succeeded =
            handle.Status == AsyncOperationStatus.Succeeded &&
            !string.IsNullOrEmpty(handle.Result);

        string localizedValue =
            succeeded ? handle.Result : fallback;

        Addressables.Release(handle);

        // 페이지를 넘기는 동안 이전 번역 요청이 끝난 경우를 방지한다.
        if (target == null || target.text != fallback)
            yield break;

        if (succeeded)
        {
            target.text = localizedValue;
        }
        else
        {
            Debug.LogWarning(
                $"[MiniDoGam] 번역 키를 찾을 수 없음: {key}"
            );
        }
    }

    private static string GetRecipeFallback(
        DoGamEntry entry,
        int index)
    {
        if (entry?.recipe != null &&
            index >= 0 &&
            index < entry.recipe.Count)
        {
            return entry.recipe[index];
        }

        return string.Empty;
    }

    private static string GetRecipeKey(
        DoGamEntry entry,
        int index)
    {
        if (entry?.recipeKeys != null &&
            index >= 0 &&
            index < entry.recipeKeys.Count)
        {
            return entry.recipeKeys[index];
        }

        return string.Empty;
    }

    private void Start()
    {
        _currentCategory =
            string.IsNullOrEmpty(defaultCategory)
                ? "떡"
                : defaultCategory;

        if (tteokTabButton != null)
        {
            _tteokNormalSprite =
                tteokTabButton.image != null
                    ? tteokTabButton.image.sprite
                    : null;
        }

        if (drinkTabButton != null)
        {
            _drinkNormalSprite =
                drinkTabButton.image != null
                    ? drinkTabButton.image.sprite
                    : null;
        }

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(
                () => ChangeIndex(-1)
            );
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(
                () => ChangeIndex(1)
            );
        }

        DisableButtonNavigation(prevButton);
        DisableButtonNavigation(nextButton);
        DisableButtonNavigation(tteokTabButton);
        DisableButtonNavigation(drinkTabButton);
        DisableButtonNavigation(miniToggleButton);

        ClearUISelection();

        if (miniToggleButton != null &&
            miniToggleButton.image != null)
        {
            _miniToggleNormalSprite =
                miniToggleButton.image.sprite;

            SpriteState state =
                miniToggleButton.spriteState;

            _miniToggleOnSprite =
                state.selectedSprite;
        }

        if (tteokTabButton != null)
        {
            tteokTabButton.onClick.AddListener(
                () => OnClickCategory("떡")
            );
        }

        if (drinkTabButton != null)
        {
            drinkTabButton.onClick.AddListener(
                () => OnClickCategory("음료")
            );
        }

        if (infoText != null)
        {
            bool tutorialDone =
                TutorialFlowManager.Instance != null &&
                TutorialFlowManager.Instance.currentStep ==
                GlobalTutorialStep.Done;

            infoText.SetActive(tutorialDone);
        }

        ReloadList();
        UpdateCategoryTabVisual();

        if (miniRoot != null)
            miniRoot.SetActive(startVisible);

        UpdateMiniToggleVisual();
    }

    public void ForceCloseMiniPanel()
    {
        if (miniRoot == null)
            return;

        if (!miniRoot.activeSelf)
            return;

        miniRoot.SetActive(false);

        if (infoText != null)
            infoText.SetActive(false);

        UpdateMiniToggleVisual();
    }

    private void BuildMiniRecipeLinesForEntry(
        DoGamEntry entry)
    {
        if (miniContentParent == null)
            return;

        for (int childIndex =
                 miniContentParent.childCount - 1;
             childIndex >= 0;
             childIndex--)
        {
            Destroy(
                miniContentParent
                    .GetChild(childIndex)
                    .gameObject
            );
        }

        if (entry == null)
            return;

        DoGamUIManager dogam =
            DoGamUIManager.Instance;

        if (dogam == null)
            return;

        GameObject iconPrefab =
            miniRecipeImagePrefab != null
                ? miniRecipeImagePrefab
                : dogam.recipeImagePrefab;

        int recipeCount = Mathf.Max(
            entry.recipe?.Count ?? 0,
            entry.recipeKeys?.Count ?? 0
        );

        int bundleCount =
            entry.recipeImageBundle != null
                ? entry.recipeImageBundle.Count
                : 0;

        int linesWithImages =
            Mathf.Min(recipeCount, bundleCount);

        // 이미지가 포함된 제작 단계
        for (int i = 0; i < linesWithImages; i++)
        {
            RecipeImageData bundle =
                entry.recipeImageBundle[i];

            int ingredientCount = Mathf.Clamp(
                bundle?.ingredients != null
                    ? bundle.ingredients.Count
                    : 0,
                1,
                4
            );

            string backgroundPrefabName =
                $"RecipeLineMiniBG_{ingredientCount}";

            GameObject prefab =
                Resources.Load<GameObject>(
                    $"RecipeLineMini/{backgroundPrefabName}"
                );

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[MiniDoGam] 배경 프리팹 " +
                    $"{backgroundPrefabName}을 찾을 수 없습니다. " +
                    $"i={i}"
                );

                continue;
            }

            GameObject lineObject =
                Instantiate(prefab, miniContentParent);

            TextMeshProUGUI recipeLineText =
                lineObject.GetComponentInChildren<
                    TextMeshProUGUI
                >();

            SetLocalizedText(
                recipeLineText,
                GetRecipeKey(entry, i),
                GetRecipeFallback(entry, i)
            );

            Transform toolSlot =
                lineObject.transform.Find("ToolSlot");

            Transform resultSlot =
                lineObject.transform.Find("ResultSlot");

            List<Transform> ingredientSlots =
                new List<Transform>
                {
                    lineObject.transform.Find(
                        "IngredientSlot1"
                    ),
                    lineObject.transform.Find(
                        "IngredientSlot2"
                    ),
                    lineObject.transform.Find(
                        "IngredientSlot3"
                    ),
                    lineObject.transform.Find(
                        "IngredientSlot4"
                    )
                };

            // 제작기 아이콘
            if (!string.IsNullOrEmpty(bundle.tool) &&
                toolSlot != null &&
                iconPrefab != null)
            {
                GameObject iconObject =
                    Instantiate(iconPrefab, toolSlot);

                RectTransform rectTransform =
                    iconObject.GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    rectTransform.sizeDelta =
                        new Vector2(50, 50);
                }

                Image image =
                    iconObject.GetComponent<Image>();

                string toolName =
                    Path.GetFileNameWithoutExtension(
                        bundle.tool
                    );

                Sprite sprite =
                    Resources.Load<Sprite>(
                        $"Sprites/restaurant/{toolName}"
                    );

                if (image != null)
                {
                    image.sprite = sprite;
                    image.enabled = sprite != null;

                    if (sprite != null)
                        image.preserveAspect = true;
                }
            }

            // 재료 아이콘
            if (bundle.ingredients != null &&
                iconPrefab != null)
            {
                for (int j = 0;
                     j < bundle.ingredients.Count &&
                     j < ingredientSlots.Count;
                     j++)
                {
                    if (ingredientSlots[j] == null)
                        continue;

                    GameObject iconObject =
                        Instantiate(
                            iconPrefab,
                            ingredientSlots[j]
                        );

                    RectTransform rectTransform =
                        iconObject.GetComponent<
                            RectTransform
                        >();

                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta =
                            new Vector2(50, 50);
                    }

                    Image image =
                        iconObject.GetComponent<Image>();

                    string ingredientName =
                        Path.GetFileNameWithoutExtension(
                            bundle.ingredients[j]
                        );

                    Sprite sprite =
                        Resources.Load<Sprite>(
                            "Sprites/Ingredients/" +
                            ingredientName
                        );

                    if (image != null)
                    {
                        image.sprite = sprite;
                        image.enabled = sprite != null;

                        if (sprite != null)
                            image.preserveAspect = true;
                    }
                }
            }

            // 결과물 아이콘
            if (!string.IsNullOrEmpty(bundle.result) &&
                resultSlot != null &&
                iconPrefab != null)
            {
                GameObject iconObject =
                    Instantiate(iconPrefab, resultSlot);

                RectTransform rectTransform =
                    iconObject.GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    rectTransform.sizeDelta =
                        new Vector2(50, 50);
                }

                Image image =
                    iconObject.GetComponent<Image>();

                string resultName =
                    Path.GetFileNameWithoutExtension(
                        bundle.result
                    );

                Sprite sprite =
                    Resources.Load<Sprite>(
                        $"Sprites/Ingredients/{resultName}"
                    );

                if (image != null)
                {
                    image.sprite = sprite;
                    image.enabled = sprite != null;

                    if (sprite != null)
                        image.preserveAspect = true;
                }
            }
        }

        // 이미지 번들이 없는 텍스트 제작 단계
        for (int i = linesWithImages;
             i < recipeCount;
             i++)
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    "RecipeLineMini/RecipeLineMiniBG_1"
                );

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[MiniDoGam] 기본 배경 프리팹 " +
                    "RecipeLineMiniBG_1을 찾을 수 없습니다. " +
                    $"i={i}"
                );

                continue;
            }

            GameObject lineObject =
                Instantiate(prefab, miniContentParent);

            TextMeshProUGUI recipeLineText =
                lineObject.GetComponentInChildren<
                    TextMeshProUGUI
                >();

            SetLocalizedText(
                recipeLineText,
                GetRecipeKey(entry, i),
                GetRecipeFallback(entry, i)
            );
        }

        if (miniScrollRect != null)
        {
            miniScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ToggleMiniPanel()
    {
        ClearUISelection();

        if (miniRoot == null)
            return;

        DoGamUIManager dogam =
            DoGamUIManager.Instance;

        if (dogam != null && dogam.IsOpen())
        {
            if (miniRoot.activeSelf)
            {
                miniRoot.SetActive(false);
                UpdateMiniToggleVisual();
                ClearUISelection();
            }

            return;
        }

        bool newActive =
            !miniRoot.activeSelf;

        miniRoot.SetActive(newActive);

        if (infoText != null)
            infoText.SetActive(false);

        if (newActive)
        {
            ReloadList();
            UpdateCategoryTabVisual();
        }

        UpdateMiniToggleVisual();
    }

    private void UpdateTopItemUI(
        DoGamEntry entry)
    {
        if (topItemImage != null)
        {
            if (entry == null ||
                string.IsNullOrEmpty(entry.image))
            {
                topItemImage.sprite = null;
                topItemImage.enabled = false;
            }
            else
            {
                Sprite sprite =
                    Resources.Load<Sprite>(
                        "Sprites/Ingredients/" +
                        entry.image
                    );

                topItemImage.sprite = sprite;
                topItemImage.enabled = sprite != null;

                if (sprite != null)
                    topItemImage.preserveAspect = true;
            }
        }

        // 다과 이름을 DoGam 테이블에서 불러온다.
        SetLocalizedText(
            topItemName,
            entry != null
                ? entry.nameKey
                : string.Empty,
            entry != null
                ? entry.name
                : string.Empty
        );
    }

    private void OnClickCategory(string category)
    {
        if (_currentCategory == category)
            return;

        _currentCategory = category;
        _index = 0;

        ReloadList();
        UpdateCategoryTabVisual();
    }

    private void UpdateCategoryTabVisual()
    {
        if (tteokTabButton != null &&
            tteokTabButton.image != null)
        {
            SpriteState state =
                tteokTabButton.spriteState;

            if (_currentCategory == "떡" &&
                state.selectedSprite != null)
            {
                tteokTabButton.image.sprite =
                    state.selectedSprite;
            }
            else
            {
                tteokTabButton.image.sprite =
                    _tteokNormalSprite;
            }
        }

        if (drinkTabButton != null &&
            drinkTabButton.image != null)
        {
            SpriteState state =
                drinkTabButton.spriteState;

            if (_currentCategory == "음료" &&
                state.selectedSprite != null)
            {
                drinkTabButton.image.sprite =
                    state.selectedSprite;
            }
            else
            {
                drinkTabButton.image.sprite =
                    _drinkNormalSprite;
            }
        }
    }

    private void UpdateMiniToggleVisual()
    {
        if (miniToggleButton == null ||
            miniToggleButton.image == null)
        {
            return;
        }

        bool isOn =
            miniRoot != null &&
            miniRoot.activeSelf;

        SpriteState state =
            miniToggleButton.spriteState;

        if (isOn)
        {
            if (_miniToggleOnSprite != null)
            {
                miniToggleButton.image.sprite =
                    _miniToggleOnSprite;

                state.selectedSprite =
                    _miniToggleOnSprite;

                state.highlightedSprite =
                    _miniToggleOnSprite;

                state.pressedSprite =
                    _miniToggleOnSprite;
            }
        }
        else
        {
            if (_miniToggleNormalSprite != null)
            {
                miniToggleButton.image.sprite =
                    _miniToggleNormalSprite;

                state.selectedSprite =
                    _miniToggleNormalSprite;

                state.highlightedSprite =
                    _miniToggleNormalSprite;

                state.pressedSprite =
                    _miniToggleNormalSprite;
            }
        }

        miniToggleButton.spriteState = state;
    }

    public void ReloadList()
    {
        DoGamUIManager dogam =
            DoGamUIManager.Instance;

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
            .GetUnlockedEntriesByCategory(
                _currentCategory
            )
            .ToList();

        _unlockedCount =
            _entries.Count;

        _totalCount =
            dogam.GetTotalEntriesByCategory(
                _currentCategory
            );

        _hasLockedPeek =
            _unlockedCount < _totalCount;

        _maxIndex =
            _hasLockedPeek
                ? _unlockedCount
                : Mathf.Max(
                    0,
                    _unlockedCount - 1
                );

        _index =
            Mathf.Clamp(
                _index,
                0,
                _maxIndex
            );

        RefreshView();
    }

    private void ChangeIndex(int delta)
    {
        if (_totalCount == 0 &&
            _unlockedCount == 0)
        {
            return;
        }

        int newIndex =
            Mathf.Clamp(
                _index + delta,
                0,
                _maxIndex
            );

        if (newIndex == _index)
            return;

        _index = newIndex;
        RefreshView();
    }

    private void RefreshView()
    {
        DoGamUIManager dogam =
            DoGamUIManager.Instance;

        if (dogam == null ||
            miniContentParent == null)
        {
            return;
        }

        if (_totalCount == 0)
        {
            UpdateTopItemUI(null);
            BuildMiniRecipeLinesForEntry(null);

            if (lockIconObject != null)
                lockIconObject.SetActive(false);

            if (prevButton != null)
                prevButton.gameObject.SetActive(false);

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            return;
        }

        bool onLockedPeek =
            _hasLockedPeek &&
            _index == _unlockedCount;

        if (!onLockedPeek &&
            _unlockedCount > 0)
        {
            int clampedIndex =
                Mathf.Clamp(
                    _index,
                    0,
                    Mathf.Max(
                        0,
                        _unlockedCount - 1
                    )
                );

            DoGamEntry entry =
                _entries[clampedIndex];

            UpdateTopItemUI(entry);
            BuildMiniRecipeLinesForEntry(entry);

            if (lockIconObject != null)
                lockIconObject.SetActive(false);
        }
        else
        {
            UpdateTopItemUI(null);
            BuildMiniRecipeLinesForEntry(null);

            if (lockIconObject != null)
                lockIconObject.SetActive(true);
        }

        if (prevButton != null)
        {
            bool showPrevious =
                _index > 0;

            prevButton.gameObject.SetActive(
                showPrevious
            );
        }

        if (nextButton != null)
        {
            bool showNext =
                _index < _maxIndex;

            nextButton.gameObject.SetActive(
                showNext
            );
        }
    }
}