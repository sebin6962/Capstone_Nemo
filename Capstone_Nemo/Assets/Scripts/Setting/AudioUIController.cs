using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioUIController : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    void OnEnable()
    {
        masterSlider.SetValueWithoutNotify(SettingsManager.Instance.masterVolume);
        bgmSlider.SetValueWithoutNotify(SettingsManager.Instance.bgmVolume);
        sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.sfxVolume);

        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
    }

    void OnDisable()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        bgmSlider.onValueChanged.RemoveListener(OnBGMSliderChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
    }

    private void OnMasterSliderChanged(float value)
    {
        SettingsManager.Instance.masterVolume = value;
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.Master, value);
    }

    private void OnBGMSliderChanged(float value)
    {
        SettingsManager.Instance.bgmVolume = value;
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.BGM, value);
    }

    private void OnSFXSliderChanged(float value)
    {
        SettingsManager.Instance.sfxVolume = value;
        AudioSetting.Instance.SetAudioVolume(EAudioMixerType.SFX, value);
    }
}
