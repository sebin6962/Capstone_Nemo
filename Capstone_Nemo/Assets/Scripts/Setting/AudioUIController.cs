using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioUIController : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private Toggle masterToggle;
    [SerializeField] private Toggle bgmToggle;
    [SerializeField] private Toggle sfxToggle;

    void OnEnable()
    {
        if (SettingsManager.Instance == null ||
       AudioSetting.Instance == null)
        {
            Debug.LogError(
                "[AudioUIController] 오디오 관리자가 없습니다."
            );
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(SettingsManager.Instance.masterVolume);

            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(SettingsManager.Instance.bgmVolume);

            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.sfxVolume);

            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }

        if (masterToggle != null)
        {
            masterToggle.SetIsOnWithoutNotify(SettingsManager.Instance.masterMute);

            masterToggle.onValueChanged.AddListener(OnMasterToggleChanged);
        }

        if (bgmToggle != null)
        {
            bgmToggle.SetIsOnWithoutNotify(SettingsManager.Instance.bgmMute);

            bgmToggle.onValueChanged.AddListener(OnBGMToggleChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.SetIsOnWithoutNotify(SettingsManager.Instance.sfxMute);

            sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
        }

    }

    void OnDisable()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        }

        if (masterToggle != null)
        {
            masterToggle.onValueChanged.RemoveListener(OnMasterToggleChanged);
        }

        if (bgmToggle != null)
        {
            bgmToggle.onValueChanged.RemoveListener(OnBGMToggleChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(OnSFXToggleChanged);
        }
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

    private void OnMasterToggleChanged(bool isOn)
    {
        SettingsManager.Instance.masterMute = isOn;
        AudioSetting.Instance.SetMasterMute(isOn);
    }

    private void OnBGMToggleChanged(bool isOn)
    {
        SettingsManager.Instance.bgmMute = isOn;
        AudioSetting.Instance.SetBGMMute(isOn);
    }

    private void OnSFXToggleChanged(bool isOn)
    {
        SettingsManager.Instance.sfxMute = isOn;
        AudioSetting.Instance.SetSFXMute(isOn);
    }

}
