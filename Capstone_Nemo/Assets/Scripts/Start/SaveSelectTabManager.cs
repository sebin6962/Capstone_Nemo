using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SaveSelectTabManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject fileSelectPanel;
    public GameObject settingPanel;
    public GameObject exitPanel;
    public GameObject aboutPanel;

    [Header("Tab Buttons")]
    public Button btnFileSelect;
    public Button btnSetting;
    public Button btnExit;
    public Button btnAbout;

    [Header("Tab Texts")]
    public TMP_Text txtFileSelect;
    public TMP_Text txtSetting;
    public TMP_Text txtExit;
    public TMP_Text txtAbout;

    [Header("Managers")]
    public SaveSelectManager saveSelectManager;
    public Button btnGameExit;

    [Header("탭 텍스트 색")]
    public Color normalTextColor = Color.white;
    public Color selectedTextColor = Color.yellow;

    [Header("탭 텍스트 크기 효과")]
    public float normalTextScale = 1f;
    public float hoverTextScale = 1.08f;
    public float selectedTextScale = 1.12f;
    public float clickTextScale = 1f;

    [Header("선택된 탭 표시 UI")]
    public RectTransform selectedTabMarker;

    [Tooltip("선택된 버튼 기준 마커 위치 조정값입니다. 왼쪽에 두려면 X를 음수로 설정하세요.")]
    public Vector2 markerOffset = new Vector2(-30f, 0f);

    [Header("선택 마커 기준 부모")]
    public RectTransform markerLayer;

    private readonly List<string> tabOrder = new List<string>();
    private readonly HashSet<string> hoveringTabs = new HashSet<string>();

    private readonly Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, TMP_Text> tabTexts = new Dictionary<string, TMP_Text>();
    private readonly Dictionary<string, Vector3> originalTextScales = new Dictionary<string, Vector3>();

    private int currentTabIndex = 0;
    private string currentTab = "File";

    void Start()
    {
        AutoFindTexts();
        BuildTabOrder();
        BuildTabDictionaries();

        SetupTabButton(btnFileSelect, "File");
        SetupTabButton(btnSetting, "Setting");
        SetupTabButton(btnExit, "Exit");
        SetupTabButton(btnAbout, "About");

        if (btnGameExit != null)
        {
            btnGameExit.onClick.AddListener(() =>
            {
                QuitGame();
            });
        }

        DisableButtonTransitions();
        SaveOriginalTextScales();

        SwitchTab("File");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveTab(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveTab(1);
        }
    }

    private void AutoFindTexts()
    {
        if (txtFileSelect == null && btnFileSelect != null)
            txtFileSelect = btnFileSelect.GetComponentInChildren<TMP_Text>(true);

        if (txtSetting == null && btnSetting != null)
            txtSetting = btnSetting.GetComponentInChildren<TMP_Text>(true);

        if (txtExit == null && btnExit != null)
            txtExit = btnExit.GetComponentInChildren<TMP_Text>(true);

        if (txtAbout == null && btnAbout != null)
            txtAbout = btnAbout.GetComponentInChildren<TMP_Text>(true);
    }

    private void BuildTabOrder()
    {
        tabOrder.Clear();

        if (btnFileSelect != null && fileSelectPanel != null)
            tabOrder.Add("File");

        if (btnSetting != null && settingPanel != null)
            tabOrder.Add("Setting");

        if (btnExit != null && exitPanel != null)
            tabOrder.Add("Exit");

        if (btnAbout != null && aboutPanel != null)
            tabOrder.Add("About");
    }

    private void BuildTabDictionaries()
    {
        tabButtons.Clear();
        tabTexts.Clear();

        AddTabData("File", btnFileSelect, txtFileSelect);
        AddTabData("Setting", btnSetting, txtSetting);
        AddTabData("Exit", btnExit, txtExit);
        AddTabData("About", btnAbout, txtAbout);
    }

    private void AddTabData(string tabName, Button button, TMP_Text text)
    {
        if (button != null)
            tabButtons[tabName] = button;

        if (text != null)
            tabTexts[tabName] = text;
    }

    private void SetupTabButton(Button button, string tabName)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() => SwitchTab(tabName));

        TabTextButtonEffect effect = button.GetComponent<TabTextButtonEffect>();

        if (effect == null)
            effect = button.gameObject.AddComponent<TabTextButtonEffect>();

        effect.Init(this, tabName);
    }

    private void SaveOriginalTextScales()
    {
        originalTextScales.Clear();

        foreach (var pair in tabTexts)
        {
            if (pair.Value != null)
                originalTextScales[pair.Key] = pair.Value.rectTransform.localScale;
        }
    }

    private void MoveTab(int direction)
    {
        if (tabOrder.Count == 0)
            return;

        currentTabIndex += direction;

        if (currentTabIndex < 0)
            currentTabIndex = tabOrder.Count - 1;
        else if (currentTabIndex >= tabOrder.Count)
            currentTabIndex = 0;

        SwitchTab(tabOrder[currentTabIndex]);
    }

    public void SwitchTab(string tab)
    {
        currentTab = tab;

        int index = tabOrder.IndexOf(tab);
        if (index >= 0)
            currentTabIndex = index;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayBtnClickSFX();

        if (fileSelectPanel != null)
            fileSelectPanel.SetActive(tab == "File");

        if (settingPanel != null)
            settingPanel.SetActive(tab == "Setting");

        if (exitPanel != null)
            exitPanel.SetActive(tab == "Exit");

        if (aboutPanel != null)
            aboutPanel.SetActive(tab == "About");

        RefreshAllTabTextStates();

        Button activeButton = GetButtonByTabName(tab);

        MoveSelectedMarker(activeButton);

        if (EventSystem.current != null && activeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(activeButton.gameObject);
        }

        if (tab == "File" && saveSelectManager != null)
        {
            saveSelectManager.RefreshSaveSlots();
        }
    }

    private Button GetButtonByTabName(string tab)
    {
        if (tabButtons.ContainsKey(tab))
            return tabButtons[tab];

        return null;
    }

    private TMP_Text GetTextByTabName(string tab)
    {
        if (tabTexts.ContainsKey(tab))
            return tabTexts[tab];

        return null;
    }

    private void RefreshAllTabTextStates()
    {
        foreach (string tab in tabOrder)
        {
            RefreshTabTextState(tab);
        }
    }

    private void RefreshTabTextState(string tab)
    {
        TMP_Text text = GetTextByTabName(tab);

        if (text == null)
            return;

        bool isSelected = tab == currentTab;
        bool isHovering = hoveringTabs.Contains(tab);

        text.color = isSelected ? selectedTextColor : normalTextColor;

        float targetScale = normalTextScale;

        if (isSelected)
        {
            targetScale = selectedTextScale;
        }
        else if (isHovering)
        {
            targetScale = hoverTextScale;
        }

        SetTextScale(tab, targetScale);
    }

    private void SetTextScale(string tab, float scaleValue)
    {
        TMP_Text text = GetTextByTabName(tab);

        if (text == null)
            return;

        Vector3 baseScale = Vector3.one;

        if (originalTextScales.ContainsKey(tab))
            baseScale = originalTextScales[tab];

        text.rectTransform.localScale = baseScale * scaleValue;
    }

    private void MoveSelectedMarker(Button targetButton)
    {
        if (selectedTabMarker == null)
            return;

        if (targetButton == null)
        {
            selectedTabMarker.gameObject.SetActive(false);
            return;
        }

        if (markerLayer == null)
        {
            Debug.LogWarning("markerLayer가 연결되지 않았습니다.");
            return;
        }

        RectTransform targetRect = targetButton.GetComponent<RectTransform>();

        if (targetRect == null)
            return;

        selectedTabMarker.gameObject.SetActive(true);

        selectedTabMarker.SetParent(markerLayer, false);

        LayoutElement layoutElement = selectedTabMarker.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = selectedTabMarker.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;

        Canvas.ForceUpdateCanvases();

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);

        // 버튼의 왼쪽 중앙 위치
        Vector3 leftCenterWorld = (corners[0] + corners[1]) * 0.5f;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            markerLayer,
            RectTransformUtility.WorldToScreenPoint(null, leftCenterWorld),
            null,
            out localPoint
        );

        selectedTabMarker.anchorMin = new Vector2(0.5f, 0.5f);
        selectedTabMarker.anchorMax = new Vector2(0.5f, 0.5f);
        selectedTabMarker.pivot = new Vector2(0.5f, 0.5f);

        selectedTabMarker.anchoredPosition = localPoint + markerOffset;
    }

    public void OnTabPointerEnter(string tab)
    {
        hoveringTabs.Add(tab);

        if (tab != currentTab)
        {
            TMP_Text text = GetTextByTabName(tab);

            if (text != null)
            {
                text.color = normalTextColor;
                SetTextScale(tab, hoverTextScale);
            }
        }
    }

    public void OnTabPointerExit(string tab)
    {
        hoveringTabs.Remove(tab);

        RefreshTabTextState(tab);
    }

    public void OnTabPointerDown(string tab)
    {
        TMP_Text text = GetTextByTabName(tab);

        if (text == null)
            return;

        // 클릭하는 순간에는 원래 크기로 돌아감
        SetTextScale(tab, clickTextScale);
    }

    public void OnTabPointerUp(string tab)
    {
        // 클릭을 떼면 현재 상태에 맞게 다시 갱신
        // 실제 선택 처리는 Button.onClick -> SwitchTab()에서 처리됨
        RefreshTabTextState(tab);
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    private void DisableButtonTransitions()
    {
        if (btnFileSelect != null)
            btnFileSelect.transition = Selectable.Transition.None;

        if (btnSetting != null)
            btnSetting.transition = Selectable.Transition.None;

        if (btnExit != null)
            btnExit.transition = Selectable.Transition.None;

        if (btnAbout != null)
            btnAbout.transition = Selectable.Transition.None;
    }
}