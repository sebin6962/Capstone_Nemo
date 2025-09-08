using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject screenPanel;
    [SerializeField] private GameObject keyPanel;

    void Awake()
    {
        ApplyStoredUIScale();
    }

    void Start()
    {
        ShowAudioPanel();
    }

    public void ShowAudioPanel()
    {
        audioPanel.SetActive(true);
        screenPanel.SetActive(false);
        keyPanel.SetActive(false);
    }

    public void ShowScreenPanel()
    {
        audioPanel.SetActive(false);
        screenPanel.SetActive(true);
        keyPanel.SetActive(false);
    }

    public void ShowKeyPanel()
    {
        audioPanel.SetActive(false);
        screenPanel.SetActive(false);
        keyPanel.SetActive(true);
    }


    void ApplyStoredUIScale()
    {
        float storedScale = PlayerPrefs.GetFloat("UIScale", 1.0f);

        SettingsManager.Instance.UIScale = storedScale;

        UIInitializer[] uiInitializers = FindObjectsOfType<UIInitializer>();
        foreach (var initializer in uiInitializers)
        {
            initializer.ApplySettings();
        }

        Debug.Log($"[UIManager] 모든 Canvas에 저장된 UI 스케일({storedScale}) 적용 완료");
    }

}
