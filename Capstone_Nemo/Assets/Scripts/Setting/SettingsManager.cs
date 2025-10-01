using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-999)]

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
     static void EnsureCreated()
    {
        if (Instance == null)
        {
            //아니 이거 prefab에서는 못불러오고 Resources에서만 불러올수있음 내가 방법을 못찾는건가?
            GameObject prefab = Resources.Load<GameObject>("SettingsManager");
            if (prefab != null)
            {
                GameObject obj = Object.Instantiate(prefab);
                obj.name = "SettingsManager"; // Clone 제거
                Debug.Log("[SettingsManager] 자동 생성됨 (Resources)");
            }
            else
            {
                Debug.LogError("SettingsManager 프리팹을 찾을 수 없습니다!");
            }
        }
    }

    public float masterVolume = 1f;
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    public float UIScale = 1f;
    public float brightness = 1f;


    public bool isFullScreen = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("UIScale", UIScale);
        PlayerPrefs.SetInt("IsFullScreen", isFullScreen ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"설정 저장됨");
    }

    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        UIScale = PlayerPrefs.GetFloat("UIScale", 1f);
        isFullScreen = PlayerPrefs.GetInt("IsFullScreen", 1) == 1;

        if (AudioSetting.Instance != null)
        {
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.Master, masterVolume);
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.BGM, bgmVolume);
            AudioSetting.Instance.SetAudioVolume(EAudioMixerType.SFX, sfxVolume);
        }
    }
}
