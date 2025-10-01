using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsApply : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider brightnessSlider;


    void Start()
    {
        // 저장된 값 불러와서 슬라이더 UI에 반영
        var sm = SettingsManager.Instance;
        masterVolumeSlider.value = sm.masterVolume;
        bgmVolumeSlider.value = sm.bgmVolume;
        sfxVolumeSlider.value = sm.sfxVolume;
        brightnessSlider.value = sm.brightness;
    }

    public void OnApplySettingsPressed()
    {
        //오디오 슬라이더 값 → SettingsManager에 저장
        SettingsManager.Instance.masterVolume = masterVolumeSlider.value;
        SettingsManager.Instance.bgmVolume = bgmVolumeSlider.value;
        SettingsManager.Instance.sfxVolume = sfxVolumeSlider.value;


        //저장
        SettingsManager.Instance.SaveSettings();

        Debug.Log("[설정 적용] 모든 설정이 저장되었습니다.");

        AudioSetting.Instance?.SetAudioVolume(EAudioMixerType.Master, masterVolumeSlider.value);
        AudioSetting.Instance?.SetAudioVolume(EAudioMixerType.BGM, bgmVolumeSlider.value);
        AudioSetting.Instance?.SetAudioVolume(EAudioMixerType.SFX, sfxVolumeSlider.value);
    }
}
