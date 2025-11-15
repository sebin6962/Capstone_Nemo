using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsApply : MonoBehaviour
{
    [Header("Audio Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Mute Toggles (true = Mute ON)")]
    [SerializeField] private Toggle masterMuteToggle;
    [SerializeField] private Toggle bgmMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;

    [Header("Others")]
    [SerializeField] private Slider brightnessSlider;

    private SettingsManager sm => SettingsManager.Instance;

    void OnEnable()
    {
        SyncUIFromSettings();
    }


    void Start()
    {
        SyncUIFromSettings();
    }

    private void SyncUIFromSettings()
    {
        if (sm == null) return;

        if (masterVolumeSlider) masterVolumeSlider.value = sm.masterVolume;
        if (bgmVolumeSlider) bgmVolumeSlider.value = sm.bgmVolume;
        if (sfxVolumeSlider) sfxVolumeSlider.value = sm.sfxVolume;

        if (brightnessSlider) brightnessSlider.value = sm.brightness; 

        if (masterMuteToggle) masterMuteToggle.isOn = sm.masterMute;
        if (bgmMuteToggle) bgmMuteToggle.isOn = sm.bgmMute;
        if (sfxMuteToggle) sfxMuteToggle.isOn = sm.sfxMute;
    }

    public void OnApplySettingsPressed()
    {
        SFXManager.Instance.PlayBtnClickSFX();
        if (sm == null) return;

        //UI → SettingsManager 값 반영
        if (masterVolumeSlider) sm.masterVolume = masterVolumeSlider.value;
        if (bgmVolumeSlider) sm.bgmVolume = bgmVolumeSlider.value;
        if (sfxVolumeSlider) sm.sfxVolume = sfxVolumeSlider.value;

        if (brightnessSlider) sm.brightness = brightnessSlider.value; 

        if (masterMuteToggle) sm.masterMute = masterMuteToggle.isOn;
        if (bgmMuteToggle) sm.bgmMute = bgmMuteToggle.isOn;
        if (sfxMuteToggle) sm.sfxMute = sfxMuteToggle.isOn;

        //오디오에 적용)
        sm.SendMessage("ApplyToAudio", SendMessageOptions.DontRequireReceiver);

        //저장
        sm.SaveSettings();

        Debug.Log("[설정 적용] 모든 설정이 저장되었습니다.");
    }
}
