using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BrightnessApply : MonoBehaviour
{
    [SerializeField] private Image brightnessPanel;
    [SerializeField] private float maxAlpha = 0.8f;

    private void Awake()
    {
        if (brightnessPanel == null)
            brightnessPanel = GetComponent<Image>();

        if (brightnessPanel != null)
            brightnessPanel.raycastTarget = false;
    }

    private void Start()
    {
        ApplySavedBrightness();
    }

    private void OnEnable()
    {
        ApplySavedBrightness();
    }

    public void ApplySavedBrightness()
    {
        float value = 1f;

        if (SettingsManager.Instance != null)
        {
            value = SettingsManager.Instance.brightness;
        }
        else
        {
            value = PlayerPrefs.GetFloat("Brightness", 1f);
        }

        Apply(value);
    }

    public void Apply(float value)
    {
        if (brightnessPanel == null)
            return;

        value = Mathf.Clamp01(value);

        Color panelColor = brightnessPanel.color;
        panelColor.a = Mathf.Lerp(maxAlpha, 0f, value);
        brightnessPanel.color = panelColor;
    }
}
