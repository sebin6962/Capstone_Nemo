using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingBrightnessSlider : MonoBehaviour
{
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private bool saveImmediately = true;

    private void Awake()
    {
        if (brightnessSlider == null)
            brightnessSlider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        SyncSliderValue();

        if(brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }
    }

    private void OnDisable()
    {
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
        }
    }

    private void SyncSliderValue()
    {
        if (brightnessSlider == null)
            return;

        float value = 1f;

        if (SettingsManager.Instance != null)
            value = SettingsManager.Instance.brightness;
        else
            value = PlayerPrefs.GetFloat("Brightness", 1f);

        brightnessSlider.SetValueWithoutNotify(value);
    }

    private void OnBrightnessChanged(float value)
    {
        if(SettingsManager.Instance != null)
        {
            SettingsManager.Instance.brightness = value;

            if (saveImmediately)
                SettingsManager.Instance.SaveSettings();
        }

        else
        {
            PlayerPrefs.SetFloat("Brightness", value);
            PlayerPrefs.Save();
        }

         BrightnessApply[] appliers = FindObjectsOfType<BrightnessApply>(true);

        foreach (BrightnessApply applier in appliers)
        {
            applier.Apply(value);
        }
    }
}
