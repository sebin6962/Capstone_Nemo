using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSetting : MonoBehaviour
{
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private Image brightnessPanel;
    [SerializeField] private Slider brightnessSlider;

    void Start()
    {
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveListener(SetBrightness);
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        ApplySavedBrightness();
    }

    private void ApplySavedBrightness()
    {
        if (SettingsManager.Instance == null)
            return;

        float savedBrightness = SettingsManager.Instance.brightness;

        if (brightnessSlider != null)
        {
            brightnessSlider.SetValueWithoutNotify(savedBrightness);
        }

        if (brightnessPanel != null)
        {
            float maxAlpha = 0.8f;
            Color panelColor = brightnessPanel.color;
            panelColor.a = Mathf.Lerp(maxAlpha, 0f, savedBrightness);
            brightnessPanel.color = panelColor;
        }
    }

    public void SetUISize_Small()
    {
        SetUIScale(0.75f);
    }

    public void SetUISize_Normal()
    {
        SetUIScale(1.0f);
    }

    public void SetUISize_Large()
    {
        SetUIScale(1.15f);
    }

    public void SetUIScale(float scale)
    {
        Debug.Log($"[SetUIScale] {scale}");
        SettingsManager.Instance.UIScale = scale;

        foreach (var ui in FindObjectsOfType<UIInitializer>())
        {
            Debug.Log($"[SetUIScale] 적용 대상: {ui.name}");
            ui.SendMessage("ApplySettings");
        }
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }

    public void SetWindowMode()
    {
        Screen.fullScreen = false;
        SettingsManager.Instance.isFullScreen = false;
        Debug.Log("[화면 모드] 창모드로 전환됨");
    }

    public void SetFullscreenMode()
    {
        Screen.fullScreen = true;
        SettingsManager.Instance.isFullScreen = true;
        Debug.Log("[화면 모드] 전체화면으로 전환됨");
    }

    public void SetBrightness(float value)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.brightness = value;
        }

        if (brightnessPanel != null)
        {
            float maxAlpha = 0.8f;
            Color panelColor = brightnessPanel.color;
            panelColor.a = Mathf.Lerp(maxAlpha, 0f, value);
            brightnessPanel.color = panelColor;
        }

        BrightnessApply[] appliers = FindObjectsOfType<BrightnessApply>(true);

        foreach (BrightnessApply applier in appliers)
        {
            applier.Apply(value);
        }
    }

    private void OnEnable()
    {
        SyncBrightnessSlider();
    }

    private void SyncBrightnessSlider()
    {
        if (brightnessSlider == null)
            return;

        float savedBrightness = 1f;

        if (SettingsManager.Instance != null)
        {
            savedBrightness = SettingsManager.Instance.brightness;
        }
        else
        {
            savedBrightness = PlayerPrefs.GetFloat("Brightness", 1f);
        }

        brightnessSlider.SetValueWithoutNotify(savedBrightness);
    }
}
