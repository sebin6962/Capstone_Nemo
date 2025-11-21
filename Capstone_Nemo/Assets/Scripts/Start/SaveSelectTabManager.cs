using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public SaveSelectManager saveSelectManager; // 추가 연결
    public Button btnGameExit;

    [Header("Sprites (탭 스프라이트)")]
    public Sprite normalSprite;
    public Sprite pressedSprite;

    [Header("Sprites (새 파일 탭 전용 스프라이트)")]
    public Sprite aboutNormalSprite;
    public Sprite aboutPressedSprite;

    private Button _activeTabButton = null;

    public Color selectedColor = Color.gray;
    public Color normalColor = Color.white;

    void Start()
    {
        btnFileSelect.onClick.AddListener(() => SwitchTab("File"));
        btnSetting.onClick.AddListener(() => SwitchTab("Setting"));
        btnExit.onClick.AddListener(() => SwitchTab("Exit"));

        if (btnAbout != null)
            btnAbout.onClick.AddListener(() => SwitchTab("About"));

        SwitchTab("File");  // 기본값: 파일 선택 탭

        btnGameExit.onClick.AddListener(() =>
        {
            QuitGame();
        });

        DisableButtonTransitions();
    }

    void DisableButtonTransitions()
    {
        if (btnFileSelect) btnFileSelect.transition = Selectable.Transition.None;
        if (btnSetting) btnSetting.transition = Selectable.Transition.None;
        if (btnExit) btnExit.transition = Selectable.Transition.None;
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    public void SwitchTab(string tab)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayBtnClickSFX();

        fileSelectPanel.SetActive(tab == "File");
        if (aboutPanel != null)
            aboutPanel.SetActive(tab == "About");
        settingPanel.SetActive(tab == "Setting");
        exitPanel.SetActive(tab == "Exit");

        SetActiveTabByName(tab);

        //if (tab == "File")
        //{
        //    SetActiveTab(btnFileSelect);
        //    // 파일 탭 진입 때 슬롯 최신화(선택)
        //    if (saveSelectManager != null)
        //        saveSelectManager.RefreshSaveSlots();
        //}
        //else if (tab == "Setting")
        //{
        //    SetActiveTab(btnSetting);
        //}
        //else // "Exit"
        //{
        //    SetActiveTab(btnExit);
        //}

        // 파일 탭 들어갈 때 슬롯 갱신
        if (tab == "File" && saveSelectManager != null)
            saveSelectManager.RefreshSaveSlots();
    }

    private void SetActiveTabByName(string tab)
    {
        // 1) 전부 normal 스프라이트로 초기화
        if (btnFileSelect && btnFileSelect.image)
            btnFileSelect.image.sprite = normalSprite;
        if (btnSetting && btnSetting.image)
            btnSetting.image.sprite = normalSprite;
        if (btnExit && btnExit.image)
            btnExit.image.sprite = normalSprite;

        if (btnAbout && btnAbout.image)
            btnAbout.image.sprite = aboutNormalSprite;

        // 2) 현재 탭만 pressed 스프라이트 적용
        if (tab == "File" && btnFileSelect && btnFileSelect.image)
            btnFileSelect.image.sprite = pressedSprite;
        else if (tab == "Setting" && btnSetting && btnSetting.image)
            btnSetting.image.sprite = pressedSprite;
        else if (tab == "Exit" && btnExit && btnExit.image)
            btnExit.image.sprite = pressedSprite;
        else if (tab == "File2" && btnAbout && btnAbout.image)
            btnAbout.image.sprite = aboutPressedSprite;
    }


    private void SetActiveTab(Button b)
    {
        // 모든 탭을 normal로 되돌리기
        if (btnFileSelect && btnFileSelect.image) btnFileSelect.image.sprite = normalSprite;
        if (btnSetting && btnSetting.image) btnSetting.image.sprite = normalSprite;
        if (btnExit && btnExit.image) btnExit.image.sprite = normalSprite;

        // 현재 탭만 pressed 스프라이트 적용
        _activeTabButton = b;
        if (_activeTabButton && _activeTabButton.image)
            _activeTabButton.image.sprite = pressedSprite;
    }

    private void SetButtonColor(Button button, bool isSelected)
    {
        var colors = button.colors;
        colors.normalColor = isSelected ? selectedColor : normalColor;
        colors.selectedColor = isSelected ? selectedColor : normalColor;
        button.colors = colors;
    }
}
