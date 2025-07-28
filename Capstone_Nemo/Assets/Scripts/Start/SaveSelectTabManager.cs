using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveSelectTabManager : MonoBehaviour
{
    public GameObject fileSelectPanel;
    public GameObject settingPanel;
    public GameObject exitPanel;

    public Button btnFileSelect;
    public Button btnSetting;
    public Button btnExit;

    public SaveSelectManager saveSelectManager; // 추가 연결
    public Button btnGameExit;

    public Color selectedColor = Color.gray;
    public Color normalColor = Color.white;

    void Start()
    {
        btnFileSelect.onClick.AddListener(() => SwitchTab("File"));
        btnSetting.onClick.AddListener(() => SwitchTab("Setting"));
        btnExit.onClick.AddListener(() => SwitchTab("Exit"));

        SwitchTab("File");  // 기본값: 파일 선택 탭

        btnGameExit.onClick.AddListener(() =>
        {
            QuitGame();
        });
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    public void SwitchTab(string tab)
    {
        fileSelectPanel.SetActive(tab == "File");
        settingPanel.SetActive(tab == "Setting");
        exitPanel.SetActive(tab == "Exit");

        SetButtonColor(btnFileSelect, tab == "File");
        SetButtonColor(btnSetting, tab == "Setting");
        SetButtonColor(btnExit, tab == "Exit");
    }
    private void SetButtonColor(Button button, bool isSelected)
    {
        var colors = button.colors;
        colors.normalColor = isSelected ? selectedColor : normalColor;
        colors.selectedColor = isSelected ? selectedColor : normalColor;
        button.colors = colors;
    }
}
