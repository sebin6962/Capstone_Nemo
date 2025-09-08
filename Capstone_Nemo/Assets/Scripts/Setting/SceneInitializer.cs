using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public class SceneInitializer : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("[SceneInitializer] SettingsManager 인스턴스가 없습니다!");
            return;
        }

        var sm = SettingsManager.Instance;
        float userScale = Mathf.Clamp(sm.UIScale, 0.5f, 2f);

        foreach (var ui in FindObjectsOfType<UIInitializer>())
        {
            ui.ApplySettings(userScale);
        }

        if (AudioSetting.Instance != null)
        {
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.Master, sm.masterVolume);
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.BGM, sm.bgmVolume);
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.SFX, sm.sfxVolume);
        }
        else
        {
            Debug.Log("[SceneInitializer] AudioSetting 없음 - 이 씬에서는 무시합니다.");
        }

        if (audioMixer != null)
        {
            audioMixer.SetFloat("Master", Mathf.Log10(sm.masterVolume) * 20);
            audioMixer.SetFloat("BGM", Mathf.Log10(sm.bgmVolume) * 20);
            audioMixer.SetFloat("SFX", Mathf.Log10(sm.sfxVolume) * 20);
            Debug.Log("[SceneInitializer] 오디오 설정 적용 완료");
        }
        else
        {
            Debug.LogWarning("[SceneInitializer] AudioMixer가 연결되지 않았습니다!");
        }

        Screen.fullScreen = sm.isFullScreen;

        Debug.Log("[SceneInitializer] 설정 적용 완료");
    }
}
