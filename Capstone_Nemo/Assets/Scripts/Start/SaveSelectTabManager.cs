using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SaveSelectTabManager : MonoBehaviour
{
    public GameObject fileSelectPanel;
    public GameObject settingPanel;
    public GameObject exitPanel;
    public GameObject aboutPanel;

    public Button btnFileSelect;
    public Button btnSetting;
    public Button btnExit;
    public Button btnAbout;

    public SaveSelectManager saveSelectManager;
    public Button btnGameExit;

    [Header("Sprites (탭 스프라이트)")]
    public Sprite normalSprite;
    public Sprite pressedSprite;

    [Header("Sprites (새 파일 탭 전용 스프라이트)")]
    public Sprite aboutNormalSprite;
    public Sprite aboutPressedSprite;

    [Header("선택된 탭 표시 UI")]
    public RectTransform selectedTabMarker;

    [Tooltip("선택된 버튼 기준 마커 위치 조정값입니다. 왼쪽에 두려면 X를 음수로 설정하세요.")]
    public Vector2 markerOffset = new Vector2(-80f, 0f);

    public Color selectedColor = Color.gray;
    public Color normalColor = Color.white;

    private readonly List<string> tabOrder = new List<string>();

    private int currentTabIndex = 0;
    private string currentTab = "File";

    [Header("선택 마커 기준 부모")]
    public RectTransform markerLayer;

    void Start()
    {
        BuildTabOrder();

        if (btnFileSelect != null)
            btnFileSelect.onClick.AddListener(() => SwitchTab("File"));

        if (btnSetting != null)
            btnSetting.onClick.AddListener(() => SwitchTab("Setting"));

        if (btnExit != null)
            btnExit.onClick.AddListener(() => SwitchTab("Exit"));

        if (btnAbout != null)
            btnAbout.onClick.AddListener(() => SwitchTab("About"));

        if (btnGameExit != null)
        {
            btnGameExit.onClick.AddListener(() =>
            {
                QuitGame();
            });
        }

        DisableButtonTransitions();

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

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
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

        SetActiveTabByName(tab);

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
        switch (tab)
        {
            case "File":
                return btnFileSelect;

            case "Setting":
                return btnSetting;

            case "Exit":
                return btnExit;

            case "About":
                return btnAbout;
        }

        return null;
    }

    private void SetActiveTabByName(string tab)
    {
        SetButtonSprite(btnFileSelect, normalSprite);
        SetButtonSprite(btnSetting, normalSprite);
        SetButtonSprite(btnExit, normalSprite);

        if (btnAbout != null)
        {
            Sprite normalAbout = aboutNormalSprite != null ? aboutNormalSprite : normalSprite;
            SetButtonSprite(btnAbout, normalAbout);
        }

        if (tab == "File")
        {
            SetButtonSprite(btnFileSelect, pressedSprite);
        }
        else if (tab == "Setting")
        {
            SetButtonSprite(btnSetting, pressedSprite);
        }
        else if (tab == "Exit")
        {
            SetButtonSprite(btnExit, pressedSprite);
        }
        else if (tab == "About")
        {
            Sprite pressedAbout = aboutPressedSprite != null ? aboutPressedSprite : pressedSprite;
            SetButtonSprite(btnAbout, pressedAbout);
        }
    }

    private void SetButtonSprite(Button button, Sprite sprite)
    {
        if (button == null)
            return;

        if (button.image == null)
            return;

        if (sprite == null)
            return;

        button.image.sprite = sprite;
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

        // Layout Group의 영향을 받지 않는 별도 레이어로 고정
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

    void DisableButtonTransitions()
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

    private void SetButtonColor(Button button, bool isSelected)
    {
        if (button == null)
            return;

        var colors = button.colors;
        colors.normalColor = isSelected ? selectedColor : normalColor;
        colors.selectedColor = isSelected ? selectedColor : normalColor;
        button.colors = colors;
    }
}